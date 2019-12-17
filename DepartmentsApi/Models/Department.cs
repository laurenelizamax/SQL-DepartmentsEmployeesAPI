using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// must have using dataAnnotations
using System.ComponentModel.DataAnnotations;

namespace DepartmentsApi.Models
{
    public class Department
    {
        public int Id { get; set; }

        // Required goes above the field that is required
        [Required(ErrorMessage = "Department Name is required")]
        [StringLength(10,MinimumLength = 2, ErrorMessage = "Department Name should be between 2 and 10 characters")]
        public string DepartmentName { get; set; }

        public List<Employee> employees { get; set; } = new List<Employee>();
    }
}
