/**
 * RingTryOn — 3D Ring Try-On Engine
 * Sử dụng Three.js + MediaPipe Hands để render model nhẫn 3D
 * định vị chính xác trên ngón trỏ, xoay theo tọa độ 3D xyz.
 */
(function () {
    'use strict';

    // ============================================================
    // CONSTANTS
    // ============================================================
    const MODEL_PATH = '/models3D/nhan3D1.glb';
    const LOAD_TIMEOUT_MS = 60000; // 60s cho file lớn
    const SMOOTHING_FACTOR = 0.35; // 0 = no smooth, 1 = freeze (position/scale)
    const ROTATION_SMOOTHING = 0.3; // slerp factor cho quaternion
    const HAND_GONE_FRAMES = 8;    // số frame không thấy tay thì ẩn model
    const RING_SCALE_MULTIPLIER = 1.0; // tuỳ chỉnh kích thước nhẫn (dựa trên finger width)

    // MediaPipe Hand Landmark indices
    const LM = {
        WRIST: 0,
        INDEX_MCP: 5,
        INDEX_PIP: 6,
        INDEX_DIP: 7,
        MIDDLE_MCP: 9,
        RING_MCP: 13,
        PINKY_MCP: 17,
        THUMB_CMC: 1
    };

    // ============================================================
    // STATE
    // ============================================================
    let renderer, scene, camera3D;
    let ringModel = null;
    let modelLoaded = false;
    let modelLoadError = null;
    let handMissingCount = 0;

    // Smoothed values (initialized in init() after THREE is available)
    let smoothPos = null;
    let smoothQuat = null;
    let smoothScale = 1;
    let firstFrame = true;

    // DOM refs (set in init)
    let containerEl, videoEl, statusEl, toastEl, loadingEl, threejsCanvas;

    // Camera stream ref
    let mediapipeHands = null;
    let cameraUtil = null;
    let isRunning = false;

    // ============================================================
    // INIT
    // ============================================================
    function init(config) {
        containerEl = document.getElementById('arCameraContainer');
        videoEl = document.getElementById('arVideo');
        statusEl = document.getElementById('arStatus');
        toastEl = document.getElementById('arToast');
        loadingEl = document.getElementById('arLoading');

        // Initialize THREE objects
        smoothPos = new THREE.Vector3();
        smoothQuat = new THREE.Quaternion();

        // Check WebGL support
        if (!checkWebGL()) {
            showToast('Trình duyệt của bạn không hỗ trợ WebGL. Không thể thử nhẫn 3D.', 'error', 10000);
            return false;
        }

        setupThreeJS();
        loadModel();
        setupMediaPipe();

        return true;
    }

    // ============================================================
    // WEBGL CHECK
    // ============================================================
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
        // Renderer — transparent background overlay trên video
        renderer = new THREE.WebGLRenderer({
            alpha: true,
            antialias: true,
            premultipliedAlpha: false
        });
        renderer.setPixelRatio(window.devicePixelRatio);
        renderer.setClearColor(0x000000, 0);
        renderer.outputColorSpace = THREE.SRGBColorSpace;
        renderer.toneMapping = THREE.ACESFilmicToneMapping;
        renderer.toneMappingExposure = 1.8;

        threejsCanvas = renderer.domElement;
        threejsCanvas.id = 'threejsOverlay';
        threejsCanvas.style.cssText =
            'position:absolute;top:0;left:0;width:100%;height:100%;pointer-events:none;transform:scaleX(-1);z-index:5;';

        const overlayContainer = document.getElementById('arOverlayContainer');
        if (overlayContainer) {
            overlayContainer.appendChild(threejsCanvas);
        }

        // Scene
        scene = new THREE.Scene();

        // Camera — orthographic giúp dễ map tọa độ 2D->3D
        camera3D = new THREE.OrthographicCamera(-1, 1, 1, -1, 0.01, 1000);
        camera3D.position.set(0, 0, 5);
        camera3D.lookAt(0, 0, 0);

        // === LIGHTING — sáng hơn, tự nhiên hơn ===
        // Hemisphere light: mô phỏng trời (trên) + mặt đất (dưới)
        const hemiLight = new THREE.HemisphereLight(0xffffff, 0xb0b0b0, 1.5);
        scene.add(hemiLight);

        // Ambient light phụ
        const ambient = new THREE.AmbientLight(0xffffff, 1.2);
        scene.add(ambient);

        // Key light (ánh sáng chính - từ phía trước/trên)
        const keyLight = new THREE.DirectionalLight(0xffffff, 2.0);
        keyLight.position.set(2, 4, 6);
        scene.add(keyLight);

        // Fill light (ánh sáng phụ - từ phía ngược lại)
        const fillLight = new THREE.DirectionalLight(0xfff8ee, 1.0);
        fillLight.position.set(-3, 1, 4);
        scene.add(fillLight);

        // Back light (ánh sáng viền - từ phía sau)
        const backLight = new THREE.DirectionalLight(0xffffff, 0.8);
        backLight.position.set(0, 2, -4);
        scene.add(backLight);

        // Bottom fill (tránh bóng tối dưới nhẫn)
        const bottomFill = new THREE.DirectionalLight(0xffffff, 0.5);
        bottomFill.position.set(0, -3, 2);
        scene.add(bottomFill);
    }

    // ============================================================
    // MODEL LOADING
    // ============================================================
    function loadModel() {
        showLoading(true);
        setStatus('Đang tải model 3D...', 'loading');
        modelLoadError = null;

        const loader = new THREE.GLTFLoader();

        // Timeout
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
            // onLoad
            (gltf) => {
                clearTimeout(timeoutId);

                ringModel = gltf.scene;

                // Normalize scale dựa trên bounding box
                const box = new THREE.Box3().setFromObject(ringModel);
                const size = new THREE.Vector3();
                box.getSize(size);
                const maxDim = Math.max(size.x, size.y, size.z);
                if (maxDim > 0) {
                    const normalizeScale = 1.0 / maxDim;
                    ringModel.scale.setScalar(normalizeScale);
                }

                // Center model
                const box2 = new THREE.Box3().setFromObject(ringModel);
                const center = new THREE.Vector3();
                box2.getCenter(center);
                ringModel.position.sub(center);

                // Xoay ngang: model gốc dựng dọc (lỗ nhẫn hướng lên)
                // → xoay -90° trục X để lỗ nhẫn hướng theo ngón tay
                ringModel.rotation.x = -Math.PI / 2;

                // Wrap trong group để dễ transform
                const group = new THREE.Group();
                group.add(ringModel);
                ringModel = group;

                ringModel.visible = false;
                scene.add(ringModel);

                // Enhance materials — fix mặt trong bị thấy + tăng sáng
                ringModel.traverse((child) => {
                    if (child.isMesh) {
                        child.castShadow = true;
                        child.receiveShadow = true;

                        // Material có thể là array (multi-material) hoặc single
                        const mats = Array.isArray(child.material)
                            ? child.material
                            : [child.material];

                        mats.forEach((mat) => {
                            if (!mat) return;
                            // CHỈ render mặt ngoài → ẩn mặt trong hoàn toàn
                            mat.side = THREE.FrontSide;
                            mat.depthWrite = true;
                            mat.transparent = false;
                            // Tăng độ sáng
                            mat.envMapIntensity = 2.0;
                            if (mat.metalness !== undefined) {
                                mat.metalness = Math.min(mat.metalness, 0.85);
                                mat.roughness = Math.max(mat.roughness, 0.15);
                            }
                            mat.needsUpdate = true;
                        });
                    }
                });

                modelLoaded = true;
                showLoading(false);
                setStatus('Đưa tay vào camera để thử nhẫn', 'ready');
                console.log('[RingTryOn] Model loaded successfully.');
            },
            // onProgress
            (xhr) => {
                if (xhr.total > 0) {
                    const pct = Math.round((xhr.loaded / xhr.total) * 100);
                    setStatus(`Đang tải model... ${pct}%`, 'loading');
                }
            },
            // onError
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
        mediapipeHands = new Hands({
            locateFile: (file) => `https://cdn.jsdelivr.net/npm/@mediapipe/hands/${file}`
        });

        mediapipeHands.setOptions({
            maxNumHands: 1,
            modelComplexity: 1,
            minDetectionConfidence: 0.6,
            minTrackingConfidence: 0.6
        });

        mediapipeHands.onResults(onHandResults);
    }

    // ============================================================
    // HAND TRACKING RESULTS — CORE LOGIC
    // ============================================================
    function onHandResults(results) {
        // Sync renderer size
        syncRendererSize();

        if (!modelLoaded || !ringModel) {
            renderer.render(scene, camera3D);
            return;
        }

        if (results.multiHandLandmarks && results.multiHandLandmarks.length > 0) {
            const lm = results.multiHandLandmarks[0];
            handMissingCount = 0;

            // ---- POSITION: Giữa MCP và PIP ngón trỏ ----
            const mcp = lm[LM.INDEX_MCP];
            const pip = lm[LM.INDEX_PIP];

            // Vị trí đeo nhẫn: lệch về phía PIP 60% (gần đốt ngón hơn)
            // thay vì trung điểm 50-50, để nhẫn nằm đúng vị trí đeo
            const t = 0.6;
            const ringPosNorm = {
                x: mcp.x + (pip.x - mcp.x) * t,
                y: mcp.y + (pip.y - mcp.y) * t,
                z: mcp.z + (pip.z - mcp.z) * t
            };

            // Convert normalized coords (0-1) → Three.js orthographic coords (-1 to 1)
            // MediaPipe: x(0=left,1=right) y(0=top,1=bottom) — mirrored
            const targetPos = new THREE.Vector3(
                (ringPosNorm.x - 0.5) * 2,   // → [-1, 1]
                -(ringPosNorm.y - 0.5) * 2,   // → [-1, 1], flip Y
                0
            );

            // ---- ROTATION: Tính quaternion từ 3 trục ----
            const targetQuat = computeRingRotation(lm);

            // ---- SCALE: Dựa trên BỀ NGANG ngón tay (không phải chiều dài) ----
            // Ước lượng finger width = khoảng cách INDEX_MCP ↔ MIDDLE_MCP
            // (bề ngang 1 ngón ≈ khoảng cách giữa 2 gốc ngón liền kề)
            const middleMcp = lm[LM.MIDDLE_MCP];
            const fingerWidthEstimate = Math.sqrt(
                Math.pow(mcp.x - middleMcp.x, 2) +
                Math.pow(mcp.y - middleMcp.y, 2)
            );
            const targetScale = fingerWidthEstimate * RING_SCALE_MULTIPLIER;

            // ---- SMOOTHING ----
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

            // ---- APPLY ----
            ringModel.position.copy(smoothPos);
            ringModel.quaternion.copy(smoothQuat);
            ringModel.scale.setScalar(smoothScale);
            ringModel.visible = true;

            setStatus('✓ Đã nhận diện bàn tay — Nhẫn đang hiển thị', 'success');

        } else {
            // Không tìm thấy bàn tay
            handMissingCount++;
            if (handMissingCount > HAND_GONE_FRAMES) {
                ringModel.visible = false;
                firstFrame = true;
                setStatus('Đưa tay vào camera để thử nhẫn', 'ready');
            }
        }

        renderer.render(scene, camera3D);
    }

    // ============================================================
    // COMPUTE RING ROTATION — Tính quaternion 3D từ hand landmarks
    // ============================================================
    function computeRingRotation(lm) {
        // Lấy 3D positions (MediaPipe trả z tương đối)
        const mcp3 = vec3FromLM(lm[LM.INDEX_MCP]);
        const pip3 = vec3FromLM(lm[LM.INDEX_PIP]);
        const dip3 = vec3FromLM(lm[LM.INDEX_DIP]);
        const wrist3 = vec3FromLM(lm[LM.WRIST]);
        const middleMCP3 = vec3FromLM(lm[LM.MIDDLE_MCP]);

        // ---- Trục Y (up) = hướng ngón tay: MCP → PIP ----
        const axisY = new THREE.Vector3().subVectors(pip3, mcp3).normalize();

        // ---- Trục tham chiếu = hướng ngang bàn tay: INDEX_MCP → MIDDLE_MCP ----
        const handRight = new THREE.Vector3().subVectors(middleMCP3, mcp3).normalize();

        // ---- Trục Z (forward/out of finger) = cross(Y, handRight) ----
        const axisZ = new THREE.Vector3().crossVectors(axisY, handRight).normalize();

        // ---- Trục X (right) = cross(Y, Z) — đảm bảo orthogonal ----
        const axisX = new THREE.Vector3().crossVectors(axisY, axisZ).normalize();

        // Xây rotation matrix 3x3 → Matrix4
        const rotMatrix = new THREE.Matrix4();
        rotMatrix.makeBasis(axisX, axisY, axisZ);

        const quat = new THREE.Quaternion().setFromRotationMatrix(rotMatrix);

        return quat;
    }

    // Helper: Convert MediaPipe landmark → Three.Vector3
    // MediaPipe: x(0-1,left-right) y(0-1,top-bottom) z(negative=towards camera)
    function vec3FromLM(landmark) {
        return new THREE.Vector3(
            (landmark.x - 0.5) * 2,
            -(landmark.y - 0.5) * 2,
            -landmark.z * 2           // z scale factor
        );
    }

    // ============================================================
    // RENDERER SIZE SYNC
    // ============================================================
    function syncRendererSize() {
        const w = videoEl.videoWidth || 640;
        const h = videoEl.videoHeight || 480;

        if (renderer.domElement.width !== w || renderer.domElement.height !== h) {
            renderer.setSize(w, h, false);

            // Update orthographic camera aspect
            const aspect = w / h;
            camera3D.left = -aspect;
            camera3D.right = aspect;
            camera3D.top = 1;
            camera3D.bottom = -1;
            camera3D.updateProjectionMatrix();
        }
    }

    // ============================================================
    // START / STOP
    // ============================================================
    function start() {
        if (isRunning) return;
        isRunning = true;
        firstFrame = true;
        handMissingCount = 999;

        containerEl.style.display = 'block';
        document.getElementById('btnStartAR').style.display = 'none';

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
                width: 640,
                height: 480
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

        containerEl.style.display = 'none';
        document.getElementById('btnStartAR').style.display = 'block';

        // Clean canvas
        if (renderer) {
            renderer.clear();
        }

        if (ringModel) {
            ringModel.visible = false;
        }

        firstFrame = true;
        setStatus('', 'idle');
    }

    function dispose() {
        stop();

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
        statusEl.style.display = text ? 'block' : 'none';
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
        dispose: dispose
    };

})();
