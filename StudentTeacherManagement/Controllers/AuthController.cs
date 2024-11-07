﻿using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using StudentTeacherManagement.Core.Interfaces;
using StudentTeacherManagement.Core.Models;
using StudentTeacherManagement.DTOs;

namespace StudentTeacherManagement.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
   private readonly IAuthService _authService;
   private readonly IMapper _mapper;

   public AuthController(IAuthService authService, IMapper mapper)
   {
      _authService = authService;
      _mapper = mapper;
   }

   [HttpPost("register")]
   public async Task<ActionResult> Register([FromBody] RegisterDTO user)
   {
      await _authService.Register(_mapper.Map<Student>(user));
      return Ok();
   }

   [HttpPost("verifyUser")]
   public async Task<ActionResult<User>> VerifyUser([FromBody] VerificationData verificationData)
   {
      var user = await _authService.ValidateAccount(verificationData.Email, verificationData.Code);
      return Ok(user);
   }

   [HttpPost("login")]
   public async Task<ActionResult<User>> Login([FromBody] LoginDTO user)
   {
      var loggedUser = await _authService.Login(user.Email, user.Password);

      if (loggedUser == null)
         return NotFound();
      
      return Ok(loggedUser);
   }
}