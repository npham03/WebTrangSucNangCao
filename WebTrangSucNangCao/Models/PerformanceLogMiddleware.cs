using System.Diagnostics;

namespace WebTrangSucNangCao.Models
{
    public class PerformanceLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceLogMiddleware> _logger;

        public PerformanceLogMiddleware(RequestDelegate next, ILogger<PerformanceLogMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Bấm giờ
            var stopwatch = Stopwatch.StartNew();

            // Cho request đi tiếp qua các middleware khác (Controller, View...)
            await _next(context);

            // Request chạy xong quay về đây, dừng đồng hồ
            stopwatch.Stop();
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            // Nếu trang nào load chậm quá 500ms thì cảnh báo log ra màn hình Console
            if (elapsedMilliseconds > 500)
            {
                _logger.LogWarning($"⚠️ CẢNH BÁO: Trang {context.Request.Path} chạy chậm vcl: {elapsedMilliseconds} ms!");
            }
            else
            {
                _logger.LogInformation($"✅ Trang {context.Request.Path} chạy mượt: {elapsedMilliseconds} ms.");
            }
        }
    }
}