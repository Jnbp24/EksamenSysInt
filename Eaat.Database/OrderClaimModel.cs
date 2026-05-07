using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Eaat.Database
{
    public class OrderClaim
    {
        [Key]
        public int Id { get; set; }   
        public Guid OrderId { get; set; }
        public string CourierName { get; set; } = default!;
        public DateTime ClaimedAt { get; set; }
    }
}
