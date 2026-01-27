using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi_Adec.Data;
using WebApi_Adec.Models.Dto;
using WebApi_Adec.Models.Entity;

namespace WebApi_Adec.Controllers
{
    //localhost:<port>/api/students -to access this controller 
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        private readonly AppDbcontext _dbContext;

        public StudentsController(AppDbcontext dbContext)
        {
            _dbContext = dbContext;
        }

        /********************************************/
        // GET: api/students
        [HttpGet]
        public async Task<IActionResult> GetStudents()
        {
            try
            {
                var allStudents = await _dbContext.Students.ToListAsync();

                if (allStudents == null || !allStudents.Any())
                {
                    return Ok(new { message = "No students found", data = new List<Student>() });
                }

                return Ok(new { message = "Students retrieved successfully", data = allStudents });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving students", error = ex.Message });
            }
        }

        /********************************************/
        // POST: api/students
        [HttpPost]
        public async Task<IActionResult> PostStudentDetail([FromBody] AddUpdtStudentDet stdDet)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if email already exists
                var existingStudent = await _dbContext.Students
                    .FirstOrDefaultAsync(s => s.Email == stdDet.Email);

                if (existingStudent != null)
                {
                    return BadRequest(new { message = "A student with this email already exists" });
                }

                var stdDetail = new Student
                {
                    Name = stdDet.Name,
                    Email = stdDet.Email,
                    EnrollmentDate = DateTime.Now
                };

                await _dbContext.Students.AddAsync(stdDetail);
                await _dbContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetStudentByid), new { id = stdDetail.Id },
                    new { message = "Student created successfully", data = stdDetail });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new { message = "Database error occurred while creating student", error = dbEx.InnerException?.Message ?? dbEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating student", error = ex.Message });
            }
        }

        /********************************************/
        // GET: api/students/{id}
        [HttpGet]
        [Route("{id:guid}")]
        public async Task<IActionResult> GetStudentByid(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest(new { message = "Invalid student ID" });
                }

                var stdDetId = await _dbContext.Students.FindAsync(id);

                if (stdDetId == null)
                {
                    return NotFound(new { message = $"Student with ID {id} not found" });
                }

                return Ok(new { message = "Student retrieved successfully", data = stdDetId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving student", error = ex.Message });
            }
        }

        /********************************************/
        // PUT: api/students/{id}
        [HttpPut]
        [Route("{id:guid}")]
        public async Task<IActionResult> PutStudentDetail([FromBody] AddUpdtStudentDet stdDet, Guid id)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (id == Guid.Empty)
                {
                    return BadRequest(new { message = "Invalid student ID" });
                }

                var stdById = await _dbContext.Students.FindAsync(id);

                if (stdById == null)
                {
                    return NotFound(new { message = $"Student with ID {id} not found" });
                }

                // Check if email is being changed to one that already exists
                if (stdById.Email != stdDet.Email)
                {
                    var emailExists = await _dbContext.Students
                        .AnyAsync(s => s.Email == stdDet.Email && s.Id != id);

                    if (emailExists)
                    {
                        return BadRequest(new { message = "A student with this email already exists" });
                    }
                }

                stdById.Name = stdDet.Name;
                stdById.Email = stdDet.Email;

                _dbContext.Students.Update(stdById);
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Student updated successfully", data = stdById });
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _dbContext.Students.AnyAsync(s => s.Id == id);
                if (!exists)
                {
                    return NotFound(new { message = $"Student with ID {id} not found" });
                }
                throw;
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new { message = "Database error occurred while updating student", error = dbEx.InnerException?.Message ?? dbEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating student", error = ex.Message });
            }
        }

        /********************************************/
        // DELETE: api/students/{id}
        [HttpDelete]
        [Route("{id:guid}")]
        public async Task<IActionResult> DeleteStudentByid(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest(new { message = "Invalid student ID" });
                }

                var stdById = await _dbContext.Students.FindAsync(id);

                if (stdById == null)
                {
                    return NotFound(new { message = $"Student with ID {id} not found" });
                }

                _dbContext.Students.Remove(stdById);
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Student deleted successfully" });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new { message = "Database error occurred while deleting student", error = dbEx.InnerException?.Message ?? dbEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting student", error = ex.Message });
            }
        }
    }
}