using System.ComponentModel.DataAnnotations;

namespace SmartOrderingSystem.Models
{
    public enum OrderStatus
    {
        [Display(Name = "Pending")]
        Pending = 0,

        [Display(Name = "In Kitchen")]
        InKitchen = 1,

        [Display(Name = "Ready")]
        Ready = 2,

        [Display(Name = "Delivered")]
        Delivered = 3
    }
}
