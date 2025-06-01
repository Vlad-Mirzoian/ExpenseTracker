using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTracker.Data.Model
{
    public class TransactionCategory
    {
        public Guid TransactionId { get; set; }
        public Guid CategoryId { get; set; }
        public bool IsBaseCategory { get; set; }
        public Transaction Transaction { get; set; }
        public Category Category { get; set; }
    }
}
