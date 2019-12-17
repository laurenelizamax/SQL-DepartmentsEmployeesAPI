using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using DepartmentsApi.Models;
using Microsoft.AspNetCore.Http;
using System;

namespace DepartmentsApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly IConfiguration _config;

        public DepartmentController(IConfiguration config)
        {
            _config = config;
        }

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }


        /// <summary>
        /// Gets a list of all Departments from database
        /// </summary>
        /// <returns> A list of Departments </returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, DeptName FROM Department";
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    List<Department> departments = new List<Department>();

                    while (reader.Read())
                    {
                        Department department = new Department
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            DepartmentName = reader.GetString(reader.GetOrdinal("DeptName"))
                        };

                        departments.Add(department);
                    }
                    reader.Close();

                    return Ok(departments);
                }
            }
        }

        /// <summary>
        /// Get by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}", Name = "GetDepartment")]
        public async Task<IActionResult> GetbyId([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT
                            Id, DeptName
                        FROM Department
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = cmd.ExecuteReader();

                    Department department = null;

                    if (reader.Read())
                    {
                        department = new Department
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            DepartmentName = reader.GetString(reader.GetOrdinal("DeptName"))
                        };
                    }
                    reader.Close();

                    if (department == null)
                    {
                        return NotFound($"No department found with the Id of {id}");
                    }

                    return Ok(department);
                }
            }
        }


        // We can make custom routes by using the "Route" annotation. NOTE: DO NOT put a leading forward slash in the URL
        [HttpGet]
        [Route("employeeCount")]
        public async Task<IActionResult> GetEmployeeCount()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT d.Id, d.DeptName, COUNT(e.Id) as EmployeeCount
                                                FROM Department d
                                                LEFT JOIN Employee e ON d.Id = e.DepartmentId
                                                GROUP BY d.Id, d.DeptName";
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    var deptEmpCount = new List<DepartmentEmployeeCount>();

                    while (reader.Read())
                    {
                        deptEmpCount.Add(new DepartmentEmployeeCount
                        {
                            DepartmentId = reader.GetInt32(reader.GetOrdinal("Id")),
                            DepartmentName = reader.GetString(reader.GetOrdinal("DeptName")),
                            EmployeeCount = reader.GetInt32(reader.GetOrdinal("EmployeeCount"))
                        });
                    }
                    reader.Close();

                    return Ok(deptEmpCount);
                }
            }
        }


        [HttpGet]
        // Need to specify the route because there are more than one HttpGet
        [Route("something/crazy")]
        //[Route("GetAllEmployeesInDept")]
        public async Task<IActionResult> GetAllEmployeesInDept()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT d.Id, d.DeptName, e.FirstName, e.LastName, 
                        e.DepartmentId, e.Id as EmployeeId
                        FROM Department d
                        LEFT JOIN Employee e ON d.Id = e.DepartmentId;";
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    List<Department> departments = new List<Department>();

                    while (reader.Read())
                    {
                        var departmentId = reader.GetInt32(reader.GetOrdinal("Id"));
                        var departmentAlreadyAdded = departments.FirstOrDefault(d => d.Id == departmentId);

                        if (departmentAlreadyAdded == null)
                        {
                            Department department = new Department
                            {
                                Id = departmentId,
                                DepartmentName = reader.GetString(reader.GetOrdinal("DeptName")),
                                employees = new List<Employee>()
                            };

                            departments.Add(department);
                         
                             var hasEmployee = !reader.IsDBNull(reader.GetOrdinal("EmployeeId"));

                            if(hasEmployee)
                            {
                                department.employees.Add(new Employee()
                                {
                                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                    DepartmentId = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Id = reader.GetInt32(reader.GetOrdinal("EmployeeId")),

                                });
                            }
                        }
                        else
                        {
                            var hasEmployee = !reader.IsDBNull(reader.GetOrdinal("EmployeeId"));

                            if (hasEmployee)
                            {
                                departmentAlreadyAdded.employees.Add(new Employee()
                                {
                                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                    DepartmentId = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Id = reader.GetInt32(reader.GetOrdinal("EmployeeId"))
                                });
                            }
                        }
                    }

                    reader.Close();

                    return Ok(departments);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Department department)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Department (DepartmentName)
                                        OUTPUT INSERTED.Id
                                        VALUES (@DeptName)";
                    cmd.Parameters.Add(new SqlParameter("@DeptName", department.DepartmentName));

                    int newId = (int) await cmd.ExecuteScalarAsync();
                    department.Id = newId;
                    return CreatedAtRoute("GetDepartment", new { id = newId }, department);
                }
            }
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Department department)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Department
                                            SET DeptName = @DeptName
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@DeptName", department.DepartmentName));
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        return BadRequest($"No department with the Id {id}");
                    }
                }
            }
            catch (Exception)
            {
                bool exists = await DepartmentExists(id);
                if (!exists)
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"DELETE FROM Department WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                bool exists = await DepartmentExists(id);
                if (!exists)
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }
        private async Task<bool> DepartmentExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, DeptName
                        FROM Department
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    return reader.Read();
                }
            }
        }
    }
}
   