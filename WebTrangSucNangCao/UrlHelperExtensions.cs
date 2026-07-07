using Microsoft.AspNetCore.Mvc;
using System;

namespace WebTrangSucNangCao
{
    public static class UrlHelperExtensions
    {
        public static string NormalizeImagePath(this IUrlHelper urlHelper, string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return "~/imgages/placeholder.png";
            }

            var normalized = imagePath.Trim();

            if (normalized.StartsWith("~/Content/Img/Products/", StringComparison.OrdinalIgnoreCase))
            {
                return "~/imgages/products/" + normalized["~/Content/Img/Products/".Length..];
            }

            if (normalized.StartsWith("Content/Img/Products/", StringComparison.OrdinalIgnoreCase))
            {
                return "~/imgages/products/" + normalized["Content/Img/Products/".Length..];
            }

            if (normalized.StartsWith("~/Content/Img/Banners/", StringComparison.OrdinalIgnoreCase))
            {
                return "~/imgages/banners/" + normalized["~/Content/Img/Banners/".Length..];
            }

            if (normalized.StartsWith("Content/Img/Banners/", StringComparison.OrdinalIgnoreCase))
            {
                return "~/imgages/banners/" + normalized["Content/Img/Banners/".Length..];
            }

            if (normalized.StartsWith("~/Content/Img/", StringComparison.OrdinalIgnoreCase))
            {
                var remainder = normalized["~/Content/Img/".Length..];
                if (remainder.StartsWith("Products/", StringComparison.OrdinalIgnoreCase))
                {
                    return "~/imgages/products/" + remainder["Products/".Length..];
                }

                if (remainder.StartsWith("Banners/", StringComparison.OrdinalIgnoreCase))
                {
                    return "~/imgages/banners/" + remainder["Banners/".Length..];
                }
            }

            return normalized;
        }
    }
}
