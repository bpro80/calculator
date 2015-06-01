using System;
using System.ComponentModel.DataAnnotations;

namespace BO
{
    public class Payment
    {
        [Display(Name = "Сумма оплаты")]
        [Required(ErrorMessage = "Введите фирму")]
        public string FirmName { get; set; }
        public int PaymentID { get; set; }
        public int CurrencyID { get; set; }
        public DateTime AddDate { get; set; }
      
        [Display(Name = "Дата оплаты")]
        [Required(ErrorMessage = "Введите дату оплаты")]
        public DateTime PayDate { get; set; }
        public Guid FirmID { get; set; }
      //  private bool IsActivated { get; set; }
        public int PayTypeID { get; set; }

        [Display(Name = "Сумма оплаты")]
        [Range(1, Int32.MaxValue, ErrorMessage = "Сумма оплаты должна быть в диапазоне от {1} до {2}")]
        [Required(ErrorMessage = "Введите сумму оплаты")]
        public Decimal Summa { get; set; }
        public string PayTypeName { get; set; }
        public string Comments { get; set; }

    }
}
