using System.Collections.Generic;

namespace WebTrangSucNangCao.Models
{
    public class HomeViewModel
    {
        public List<Slider> Sliders { get; set; }
        public List<ProductHomeViewModel> Products { get; set; }
    }
}