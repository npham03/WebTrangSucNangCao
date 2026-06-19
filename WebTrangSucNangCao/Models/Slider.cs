using System;
using System.Collections.Generic;

namespace WebTrangSucNangCao.Models;

public partial class Slider
{
    public int SliderId { get; set; }

    public string? ImagePath { get; set; }

    public string? Title { get; set; }

    public string? Link { get; set; }
}
