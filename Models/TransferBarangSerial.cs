using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyGudang.Models
{
    public class TransferBarangSerial
    {
        public int Id { get; set; }
        
        public int TransferBarangId { get; set; }
        
        [ForeignKey("TransferBarangId")]
        public TransferBarang? TransferBarang { get; set; }
        
        public int BarangSerialId { get; set; }
        
        [ForeignKey("BarangSerialId")]
        public BarangSerial? BarangSerial { get; set; }
    }
}
