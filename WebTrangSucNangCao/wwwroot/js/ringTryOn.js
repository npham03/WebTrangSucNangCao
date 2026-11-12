/**
 * RingTryOn — 3D Ring Try-On Engine
 * Sử dụng Three.js (Perspective + RoomEnvironment) + MediaPipe Hands
 * để render model nhẫn 3D ôm sát ngón tay trong chế độ popup toàn màn hình.
 */
(function () {
    'use strict';

    // ============================================================
    // CONSTANTS & FINGER CONFIG
    // ============================================================
    const MODEL_PATH = '/models3D/nhan3D1.glb';
    const LOAD_TIMEOUT_MS = 60000;
    const SMOOTHING_FACTOR = 0.35;
    const ROTATION_SMOOTHING = 0.3;
    const HAND_GONE_FRAMES = 8;
    const DEFAULT_SCALE_MULTIPLIER = 0.43; // Vừa vặn chu vi ngón tay theo chiều dài đốt ngón
    const CAMERA_FOV = 45;
    const CAMERA_Z = 1 / Math.tan(Math.PI / 8); // ~2.4142 => tại z=0, halfHeight = 1, halfWidth = aspect

    // Bộ landmark theo từng ngón tay (cùng quy ước MCP -> PIP -> refMCP về phía ngón út)
    const FINGERS = {
        index: { mcp: 5, pip: 6, refMcp: 9 },
        middle: { mcp: 9, pip: 10, refMcp: 13 },
        ring: { mcp: 13, pip: 14, refMcp: 17 }
    };

    // ============================================================
    // STATE
    // ============================================================
    let renderer, scene, camera3D;
    let pmremGenerator = null, envTexture = null;
    let ringModel = null;
    let fingerOccluder = null;
    let modelLoaded = false;
    let modelLoadError = null;
    let handMissingCount = 0;

    let activeFinger = 'index';
    let scaleMultiplier = DEFAULT_SCALE_MULTIPLIER;

    let smoothPos = null;
    let smoothQuat = null;
    let smoothScale = 1;
    let firstFrame = true;

    // DOM refs
    let containerEl, mediaWrapperEl, videoEl, statusEl, toastEl, loadingEl, threejsCanvas;

    // Camera / MediaPipe refs
    let mediapipeHands = null;
    let cameraUtil = null;
    let isRunning = false;
    let resizeHandler = null;

    // ============================================================
    // INIT
    // ============================================================
    function init() {
        containerEl = document.getElementById('arCameraContainer');
        mediaWrapperEl = document.getElementById('arMediaWrapper');
        videoEl = document.getElementById('arVideo');
        statusEl = document.getElementById('arStatus');
        toastEl = document.getElementById('arToast');
        loadingEl = document.getElementById('arLoading');

        smoothPos = new THREE.Vector3();
        smoothQuat = new THREE.Quaternion();

        if (!checkWebGL()) {
            showToast('Trình duyệt của bạn không hỗ trợ WebGL. Không thể thử nhẫn 3D.', 'error', 10000);
            return false;
        }

        setupThreeJS();
        loadModel();
        setupMediaPipe();

        resizeHandler = function () {
            if (isRunning) syncRendererSize();
        };
        window.addEventListener('resize', resizeHandler);

        return true;
    }

    function checkWebGL() {
        try {
            const c = document.createElement('canvas');
            return !!(c.getContext('webgl') || c.getContext('webgl2'));
        } catch (e) {
            return false;
        }
    }

    // ============================================================
    // THREE.JS SETUP
    // ============================================================
    function setupThreeJS() {
        renderer = new THREE.WebGLRenderer({
            alpha: true,
            antialias: true,
            premultipliedAlpha: false
        });
        renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
        renderer.setClearColor(0x000000, 0);
        renderer.outputColorSpace = THREE.SRGBColorSpace;
        renderer.toneMapping = THREE.ACESFilmicToneMapping;
        renderer.toneMappingExposure = 0.95; // Giữ độ tương phản kim loại, không bị cháy trắng

        threejsCanvas = renderer.domElement;
        threejsCanvas.id = 'threejsOverlay';
        threejsCanvas.style.cssText =
            'position:absolute;top:0;left:0;width:100%;height:100%;pointer-events:none;transform:scaleX(-1);z-index:5;';

        const overlayContainer = document.getElementById('arOverlayContainer');
        if (overlayContainer) {
            overlayContainer.appendChild(threejsCanvas);
        }

        scene = new THREE.Scene();

        if (typeof THREE.RoomEnvironment === 'function' && typeof THREE.PMREMGenerator === 'function') {
            pmremGenerator = new THREE.PMREMGenerator(renderer);
            envTexture = pmremGenerator.fromScene(new THREE.RoomEnvironment(), 0.04).texture;
            scene.environment = envTexture;
        }

        // PerspectiveCamera: có chiều sâu 3D tự nhiên nhưng khớp hệ tọa độ 2D tại z = 0
        camera3D = new THREE.PerspectiveCamera(CAMERA_FOV, 16 / 9, 0.05, 100);
        camera3D.position.set(0, 0, CAMERA_Z);
        camera3D.lookAt(0, 0, 0);

        // Ánh sáng cân bằng tạo khối kim loại & độ lấp lánh
        const hemiLight = new THREE.HemisphereLight(0xffffff, 0x444444, 0.45);
        scene.add(hemiLight);

        const ambient = new THREE.AmbientLight(0xffffff, 0.25);
        scene.add(ambient);

        const keyLight = new THREE.DirectionalLight(0xffffff, 1.3);
        keyLight.position.set(2, 4, 5);
        scene.add(keyLight);

        const fillLight = new THREE.DirectionalLight(0xfff5e8, 0.45);
        fillLight.position.set(-3, 1, 4);
        scene.add(fillLight);

        const rimLight = new THREE.DirectionalLight(0xdbeafe, 0.7);
        rimLight.position.set(0, 2, -4);
        scene.add(rimLight);

        // ===== FINGER OCCLUDER =====
        // Khối trụ vô hình khớp đúng đường kính lòng trong của nhẫn để che sạch nửa sau
        const occluderGeo = new THREE.CylinderGeometry(0.5, 0.5, 1, 32, 1, false);
        const occluderMat = new THREE.MeshBasicMaterial({
            colorWrite: false,
            depthWrite: true,
            depthTest: true
        });
        fingerOccluder = new THREE.Mesh(occluderGeo, occluderMat);
        fingerOccluder.renderOrder = 0;
        fingerOccluder.visible = false;
        scene.add(fingerOccluder);
    }

    // ============================================================
    // MODEL LOADING
    // ============================================================
    function loadModel() {
        showLoading(true);
        setStatus('Đang tải model 3D...', 'loading');
        modelLoadError = null;

        const loader = new THREE.GLTFLoader();

        const timeoutId = setTimeout(() => {
            if (!modelLoaded) {
                modelLoadError = 'timeout';
                showLoading(false);
                showToast('Tải model 3D quá lâu (timeout). File có thể quá lớn hoặc kết nối chậm.', 'error', 8000);
                setStatus('Lỗi tải model 3D', 'error');
            }
        }, LOAD_TIMEOUT_MS);

        loader.load(
            MODEL_PATH,
            (gltf) => {
                clearTimeout(timeoutId);

                ringModel = gltf.scene;

                const box = new THREE.Box3().setFromObject(ringModel);
                const size = new THREE.Vector3();
                box.getSize(size);
                const maxDim = Math.max(size.x, size.y, size.z);
                if (maxDim > 0) {
                    ringModel.scale.setScalar(1.0 / maxDim);
                }

                const box2 = new THREE.Box3().setFromObject(ringModel);
                const center = new THREE.Vector3();
                box2.getCenter(center);
                ringModel.position.sub(center);

                // Xoay -90° trục X để lỗ nhẫn hướng dọc theo trục Y của ngón tay
                ringModel.rotation.x = -Math.PI / 2;

                const group = new THREE.Group();
                group.add(ringModel);
                ringModel = group;

                ringModel.visible = false;
                ringModel.renderOrder = 1;
                scene.add(ringModel);

                // Giữ nguyên PBR texture gốc, cân chỉnh envMapIntensity vừa phải để không bị cháy trắng
                ringModel.traverse((child) => {
                    if (child.isMesh) {
                        const mats = Array.isArray(child.material)
                            ? child.material
                            : [child.material];

                        mats.forEach((mat) => {
                            if (!mat) return;
                            mat.side = THREE.FrontSide;
                            mat.depthWrite = true;
                            mat.transparent = false;
                            mat.envMapIntensity = 1.15;
                            if (mat.roughness !== undefined) {
                                mat.roughness = Math.max(mat.roughness, 0.08);
                            }
                            mat.needsUpdate = true;
                        });
                    }
                });

                modelLoaded = true;
                showLoading(false);
                setStatus('Đưa ngón tay vào camera để thử nhẫn', 'ready');
            },
            (xhr) => {
                if (xhr.total > 0) {
                    const pct = Math.round((xhr.loaded / xhr.total) * 100);
                    setStatus(`Đang tải model... ${pct}%`, 'loading');
                }
            },
            (error) => {
                clearTimeout(timeoutId);
                modelLoadError = error;
                modelLoaded = false;
                showLoading(false);

                let msg = 'Không thể tải model 3D. ';
                if (error && error.message) {
                    if (error.message.includes('404') || error.message.includes('Not Found')) {
                        msg += 'File model không tồn tại trên server.';
                    } else if (error.message.includes('NetworkError') || error.message.includes('Failed to fetch')) {
                        msg += 'Lỗi mạng, vui lòng kiểm tra kết nối.';
                    } else {
                        msg += 'Chi tiết: ' + error.message;
                    }
                } else {
                    msg += 'Lỗi không xác định.';
                }

                showToast(msg, 'error', 10000);
                setStatus('Lỗi tải model 3D', 'error');
                console.error('[RingTryOn] Model load error:', error);
            }
        );
    }

    // ============================================================
    // MEDIAPIPE SETUP
    // ============================================================
    function setupMediaPipe() {
        if (typeof Hands === 'undefined') {
            showToast('Không thể tải thư viện nhận diện bàn tay MediaPipe.', 'error', 8000);
            setStatus('Lỗi tải MediaPipe', 'error');
            return;
        }

        mediapipeHands = new Hands({
            locateFile: (file) => `https://cdn.jsdelivr.net/npm/@mediapipe/hands/${file}`
        });

        mediapipeHands.setOptions({
            maxNumHands: 1,
            modelComplexity: 1,
            minDetectionConfidence: 0.65,
            minTrackingConfidence: 0.65
        });

        mediapipeHands.onResults(onHandResults);
    }

    // ============================================================
    // HAND TRACKING RESULTS
    // ============================================================
    function onHandResults(results) {
        syncRendererSize();

        if (!modelLoaded || !ringModel) {
            renderer.render(scene, camera3D);
            return;
        }

        if (results.multiHandLandmarks && results.multiHandLandmarks.length > 0) {
            const lm = results.multiHandLandmarks[0];
            handMissingCount = 0;

            const aspect = (videoEl.videoWidth || 1280) / (videoEl.videoHeight || 720);
            const fCfg = FINGERS[activeFinger] || FINGERS.index;
            const mcp = lm[fCfg.mcp];
            const pip = lm[fCfg.pip];
            const refMcp = lm[fCfg.refMcp];

            // Vị trí đeo nhẫn: khoảng 46% từ gốc ngón (MCP) lên khớp giữa (PIP)
            const t = 0.46;
            const ringPosNorm = {
                x: mcp.x + (pip.x - mcp.x) * t,
                y: mcp.y + (pip.y - mcp.y) * t
            };

            const targetPos = new THREE.Vector3(
                (ringPosNorm.x - 0.5) * 2 * aspect,
                -(ringPosNorm.y - 0.5) * 2,
                0
            );

            const targetQuat = computeRingRotation(mcp, pip, refMcp, aspect);

            // Chiều dài đốt ngón trong không gian 3D chuẩn hóa -> scale nhẫn theo slider
            const mcp3 = vec3FromLM(mcp, aspect);
            const pip3 = vec3FromLM(pip, aspect);
            const phalanxLength = pip3.distanceTo(mcp3);
            const targetScale = phalanxLength * scaleMultiplier;

            if (firstFrame) {
                smoothPos.copy(targetPos);
                smoothQuat.copy(targetQuat);
                smoothScale = targetScale;
                firstFrame = false;
            } else {
                smoothPos.lerp(targetPos, SMOOTHING_FACTOR);
                smoothQuat.slerp(targetQuat, ROTATION_SMOOTHING);
                smoothScale += (targetScale - smoothScale) * SMOOTHING_FACTOR;
            }

            ringModel.position.copy(smoothPos);
            ringModel.quaternion.copy(smoothQuat);
            ringModel.scale.setScalar(smoothScale);
            ringModel.visible = true;

            // ---- FINGER OCCLUDER ----
            // Lòng trong của nhan3D1.glb có bán kính ~0.384 * scale (đường kính ~0.768).
            // Đặt trụ occluder đúng tâm nhẫn với đường kính 0.74 * smoothScale (r = 0.37)
            // giúp mặt trước của nhẫn luôn nổi lên trọn vẹn, còn nửa sau bị ngón tay che hoàn toàn.
            fingerOccluder.position.copy(smoothPos);
            fingerOccluder.quaternion.copy(smoothQuat);
            const occluderDiameter = smoothScale * 0.74;
            const segLength = phalanxLength * 1.6;
            fingerOccluder.scale.set(occluderDiameter, segLength, occluderDiameter);
            fingerOccluder.visible = true;

            setStatus('✓ Đã nhận diện bàn tay — Nhẫn đang hiển thị', 'success');
        } else {
            handMissingCount++;
            if (handMissingCount > HAND_GONE_FRAMES) {
                ringModel.visible = false;
                if (fingerOccluder) fingerOccluder.visible = false;
                firstFrame = true;
                setStatus('Đưa ngón tay vào camera để thử nhẫn', 'ready');
            }
        }

        renderer.render(scene, camera3D);
    }

    function computeRingRotation(mcp, pip, refMcp, aspect) {
        const mcp3 = vec3FromLM(mcp, aspect);
        const pip3 = vec3FromLM(pip, aspect);
        const refMCP3 = vec3FromLM(refMcp, aspect);

        const axisY = new THREE.Vector3().subVectors(pip3, mcp3);
        if (axisY.lengthSq() < 1e-6) axisY.set(0, 1, 0);
        else axisY.normalize();

        const handRight = new THREE.Vector3().subVectors(refMCP3, mcp3);
        if (handRight.lengthSq() < 1e-6) handRight.set(1, 0, 0);
        else handRight.normalize();

        const axisZ = new THREE.Vector3().crossVectors(axisY, handRight);
        if (axisZ.lengthSq() < 1e-6) axisZ.set(0, 0, 1);
        else axisZ.normalize();

        const axisX = new THREE.Vector3().crossVectors(axisY, axisZ);
        if (axisX.lengthSq() < 1e-6) axisX.set(1, 0, 0);
        else axisX.normalize();

        const rotMatrix = new THREE.Matrix4().makeBasis(axisX, axisY, axisZ);
        return new THREE.Quaternion().setFromRotationMatrix(rotMatrix);
    }

    // MediaPipe landmark.z dùng cùng đơn vị tỉ lệ với x (chiều rộng ảnh) -> nhân aspect để giữ đúng tỉ lệ 3D
    function vec3FromLM(landmark, aspect = 1) {
        return new THREE.Vector3(
            (landmark.x - 0.5) * 2 * aspect,
            -(landmark.y - 0.5) * 2,
            -landmark.z * 2 * aspect
        );
    }

    // ============================================================
    // FULLSCREEN STAGE & RENDERER SIZE SYNC
    // ============================================================
    function syncRendererSize() {
        const w = videoEl.videoWidth || 1280;
        const h = videoEl.videoHeight || 720;
        const videoAspect = w / h;

        // Căn kích thước #arMediaWrapper phủ kín toàn màn hình theo kiểu "cover"
        // để video và canvas 3D luôn khớp 1:1 từng pixel trên mọi màn hình
        if (mediaWrapperEl) {
            const viewW = window.innerWidth;
            const viewH = window.innerHeight;
            const viewAspect = viewW / viewH;

            let stageW, stageH;
            if (viewAspect > videoAspect) {
                stageW = viewW;
                stageH = viewW / videoAspect;
            } else {
                stageH = viewH;
                stageW = viewH * videoAspect;
            }
            mediaWrapperEl.style.width = Math.ceil(stageW) + 'px';
            mediaWrapperEl.style.height = Math.ceil(stageH) + 'px';
        }

        if (renderer.domElement.width !== w || renderer.domElement.height !== h) {
            renderer.setSize(w, h, false);
            camera3D.aspect = videoAspect;
            camera3D.updateProjectionMatrix();
        }
    }

    // ============================================================
    // START / STOP / CONTROLS
    // ============================================================
    function start() {
        if (isRunning) return;

        if (!mediapipeHands || typeof Camera === 'undefined') {
            showToast('Module camera hoặc nhận diện bàn tay chưa sẵn sàng. Vui lòng tải lại trang.', 'error', 8000);
            setStatus('Lỗi khởi tạo camera/MediaPipe', 'error');
            return;
        }

        isRunning = true;
        firstFrame = true;
        handMissingCount = 999;

        containerEl.style.display = 'flex';
        document.body.style.overflow = 'hidden';
        syncRendererSize();

        if (!modelLoaded && !modelLoadError) {
            setStatus('Đang tải model 3D...', 'loading');
        }

        if (!cameraUtil) {
            cameraUtil = new Camera(videoEl, {
                onFrame: async () => {
                    if (isRunning) {
                        await mediapipeHands.send({ image: videoEl });
                    }
                },
                width: 1280,
                height: 720
            });
        }

        cameraUtil.start().catch(err => {
            showToast('Không thể truy cập camera. Vui lòng cấp quyền camera cho trình duyệt!', 'error', 8000);
            setStatus('Lỗi camera', 'error');
            console.error('[RingTryOn] Camera error:', err);
            stop();
        });
    }

    function stop() {
        isRunning = false;

        if (cameraUtil) {
            cameraUtil.stop();
        }

        if (containerEl) {
            containerEl.style.display = 'none';
        }
        document.body.style.overflow = '';

        if (renderer) {
            renderer.clear();
        }

        if (ringModel) {
            ringModel.visible = false;
        }
        if (fingerOccluder) {
            fingerOccluder.visible = false;
        }

        firstFrame = true;
        setStatus('', 'idle');
    }

    function setFinger(fingerKey) {
        if (FINGERS[fingerKey]) {
            activeFinger = fingerKey;
            firstFrame = true;
        }
    }

    function setScaleMultiplier(val) {
        const num = parseFloat(val);
        if (!isNaN(num) && num >= 0.25 && num <= 0.75) {
            scaleMultiplier = num;
        }
    }

    function dispose() {
        stop();

        if (resizeHandler) {
            window.removeEventListener('resize', resizeHandler);
            resizeHandler = null;
        }

        if (envTexture) {
            envTexture.dispose();
            envTexture = null;
        }
        if (pmremGenerator) {
            pmremGenerator.dispose();
            pmremGenerator = null;
        }

        if (renderer) {
            renderer.dispose();
            renderer.forceContextLoss();
            if (renderer.domElement && renderer.domElement.parentNode) {
                renderer.domElement.parentNode.removeChild(renderer.domElement);
            }
        }

        if (scene) {
            scene.traverse((obj) => {
                if (obj.geometry) obj.geometry.dispose();
                if (obj.material) {
                    if (Array.isArray(obj.material)) {
                        obj.material.forEach(m => m.dispose());
                    } else {
                        obj.material.dispose();
                    }
                }
            });
        }

        ringModel = null;
        fingerOccluder = null;
        modelLoaded = false;
    }

    // ============================================================
    // UI HELPERS
    // ============================================================
    function showLoading(show) {
        if (loadingEl) {
            loadingEl.style.display = show ? 'flex' : 'none';
        }
    }

    function setStatus(text, type) {
        if (!statusEl) return;
        statusEl.textContent = text;
        statusEl.className = 'ar-status';
        if (type) statusEl.classList.add('ar-status--' + type);
        statusEl.style.display = text ? 'inline-flex' : 'none';
    }

    let toastTimeout = null;
    function showToast(message, type, duration) {
        if (!toastEl) return;
        duration = duration || 5000;

        toastEl.textContent = message;
        toastEl.className = 'ar-toast ar-toast--visible';
        if (type) toastEl.classList.add('ar-toast--' + type);

        if (toastTimeout) clearTimeout(toastTimeout);
        toastTimeout = setTimeout(() => {
            toastEl.classList.remove('ar-toast--visible');
        }, duration);
    }

    // ============================================================
    // PUBLIC API
    // ============================================================
    window.RingTryOn = {
        init: init,
        start: start,
        stop: stop,
        dispose: dispose,
        setFinger: setFinger,
        setScaleMultiplier: setScaleMultiplier
    };

})();
