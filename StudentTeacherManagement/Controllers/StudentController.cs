using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentTeacherManagement.Core.Interfaces;
using StudentTeacherManagement.Core.Models;
using StudentTeacherManagement.DTOs;

namespace StudentTeacherManagement.Controllers;

[ApiController]
[Route("students")]
public class StudentController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly IMapper _mapper;

    public StudentController(IStudentService studentService, IMapper mapper)
    {
        _studentService = studentService;
        _mapper = mapper;
    }

    [HttpPost]
    [LogFilter]
    [Authorize(Roles = "Teacher")]
    public async Task<ActionResult<StudentDTO>> AddStudent([FromBody] CreateStudentDTO createStudentDto)
    {
        //var studentToAdd = _mapper.Map<Student>(createStudentDto);
        //studentToAdd.CreatedAt = DateTime.UtcNow;
        
        //var student = await _studentService.AddStudent(studentToAdd);

        //return Created($"students/{student.Id}", _mapper.Map<StudentDTO>(student));
        //return Ok(createStudentDto);
        throw new ArgumentException();
    }
}