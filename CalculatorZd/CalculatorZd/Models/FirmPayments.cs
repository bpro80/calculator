using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BO;

namespace CalculatorZd.Models
{
    public class FirmPaymentsViewModel
    {
        public DateTime? DatePayBegin { get; set; }
        public DateTime? DatePayEnd { get; set; }
        public Guid? FirmID { get; set; }

        public List<Payment> FirmPayments { get; set; }
    }

    public class AddFirmPaymentViewModel : Payment
    {
        public string FirmID { get; set; }
        public List<Firm> ActiveFirms { get; set; }
    }

    internal enum PaymentType
    {
        None,
        Nal,
        BezNal,
        InternetPayment
    }
}