using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTracker.Data.Model
{
    public class CategoryParents
    {
        public Guid CategoryId { get; set; }
        public Category Category { get; set; }
        public Guid ParentCategoryId { get; set; }
        public Category ParentCategory { get; set; }
    }
}
