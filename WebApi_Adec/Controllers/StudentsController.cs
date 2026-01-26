
using Microsoft.AspNetCore.Mvc;
using WebApi_Adec.Data;
using WebApi_Adec.Models;
using WebApi_Adec.Models.Entity;

namespace WebApi_Adec.Controllers
{
    //localhost:<port>/api/students -to access this controller 
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {

        private readonly AppDbcontext _Dbcontext;
        public StudentsController(AppDbcontext dbcontext)
        {
            _Dbcontext = dbcontext;
        }

        [HttpGet]
        public IActionResult GetStudents()
        {
            var allStudents = _Dbcontext.Students.ToList();

            return Ok(allStudents);
        }

        [HttpPost]
        public IActionResult PostStudentDetail(AddUpdtStudentDet stdDet)
        {

            var StdDetail = new Student
            {
                Name = stdDet.Name,
                Email = stdDet.Email,
                EnrollmentDate = DateTime.Now
            };
            _Dbcontext.Students.Add(StdDetail);
            _Dbcontext.SaveChanges();

            return Ok(StdDetail);
        }

        [HttpGet]
        [Route("{id:guid}")]
        public IActionResult GetStudentByid(Guid id)
        {
            var stdDetId= _Dbcontext.Students.Find(id);
            if (stdDetId == null)
            {
                return NotFound();
            }
            return Ok(stdDetId);
        }

        [HttpPut]
        [Route("{id:guid}")]
        public async Task<IActionResult> PutStudentDetail(AddUpdtStudentDet stdDet,Guid id)
        {
            var StdbyId= await _Dbcontext.Students.FindAsync(id);
            if (StdbyId == null)
            {
                return NotFound();
            }
            StdbyId.Name = stdDet.Name;
            StdbyId.Email = stdDet.Email;

            _Dbcontext.SaveChanges();

            return Ok(StdbyId);
        }

        [HttpDelete]

        [Route("{id:guid}")]
        public IActionResult DeleteStudentByid(Guid id)
        {
            var stdbyId = _Dbcontext.Students.Find(id);
            if (stdbyId == null)
            {
                return NotFound();
            }

            _Dbcontext.Students.Remove(stdbyId);
            _Dbcontext.SaveChanges();
            return Ok();
        }
    }
}
