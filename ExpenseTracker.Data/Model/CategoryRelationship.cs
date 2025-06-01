using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTracker.Data.Model
{
    public class CategoryRelationship
    {
        public Guid CustomCategoryId { get; set; }
        public Guid BaseCategoryId { get; set; }
        public Category CustomCategory { get; set; }
        public Category BaseCategory { get; set; }
    }
}
