using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using api.Dtos.Projects;
using api.Helpers;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/v1/workspaces/{workspaceId:Guid}/projects")]
    [ApiController, Authorize]
    public class ProjectController(IProjectService projectService): ApiControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto dto, [FromRoute] Guid workspaceId)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var project = await projectService.CreateProjectAsync(dto, workspaceId, CurrentUserId);
            if (!project.Succeeded) return HandleResult(project);
            return CreatedAtAction(nameof(GetProjectById), new { workspaceId, projectId = project.Data!.Id }, project.Data);
        }

        [HttpDelete("{projectId:Guid}")]
        public async Task<IActionResult> DeleteProject([FromRoute] Guid workspaceId, Guid projectId)
        {
            var project = await projectService.DeleteProjectAsync(workspaceId, projectId, CurrentUserId);
            return project.Succeeded ? NoContent() : HandleResult(project);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProjects([FromRoute] Guid workspaceId, [FromQuery] ProjectQueryObject query)
        {
            var projects = await projectService.GetAllProjectAsync(workspaceId, CurrentUserId, query);
            return HandleResult(projects);
        }

        [HttpGet("{projectId:Guid}")]
        public async Task<IActionResult> GetProjectById([FromRoute] Guid workspaceId, Guid projectId)
        {
            var project = await projectService.GetProjectByIdAsync(workspaceId, projectId, CurrentUserId);
            return project.Succeeded ? Ok(project.Data) : HandleResult(project);
        }

        [HttpPatch("{projectId:Guid}")]
        public async Task<IActionResult> UpdateProject([FromRoute] Guid workspaceId, Guid projectId, [FromBody] UpdateProjectDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await projectService.UpdateProjectAsync(workspaceId, projectId, CurrentUserId, dto);
            return HandleResult(response);
        }
    }
}