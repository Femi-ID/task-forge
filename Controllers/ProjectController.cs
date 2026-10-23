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
    [Route("api/v1/workspaces")]
    [ApiController, Authorize]
    public class ProjectController(IProjectService projectService): ApiControllerBase
    {
        [HttpPost("{workspaceId:Guid}/projects")]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto dto, [FromRoute] Guid workspaceId)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var project = await projectService.CreateProjectAsync(dto, workspaceId, CurrentUserId);
            return CreatedAtAction(nameof(GetProjectById), new { workspaceId, projectId = project.Data!.Id }, project.Data);
        }

        public record DeleteProjectDto(Guid workspaceId, Guid projectId);

        [HttpDelete("{workspaceId:Guid}/projects/{projectId:Guid}")]
        public async Task<IActionResult> DeleteProject([FromRoute] DeleteProjectDto dto)
        {
            var project = await projectService.DeleteProjectAsync(dto.workspaceId, dto.projectId, CurrentUserId);
            return project.Succeeded ? NoContent() : HandleResult(project);
        }

        [HttpGet("{workspaceId:Guid}")]
        public async Task<IActionResult> GetAllProjects([FromRoute] Guid workspaceId, [FromQuery] ProjectQueryObject query)
        {
            var projects = await projectService.GetAllProjectAsync(workspaceId, CurrentUserId, query);
            return HandleResult(projects);
        }

        [HttpGet("{workspaceId:Guid}/{projectId:Guid}")]
        public async Task<IActionResult> GetProjectById([FromRoute] Guid workspaceId, Guid projectId)
        {
            var project = await projectService.GetProjectByIdAsync(workspaceId, projectId, CurrentUserId);
            return project.Succeeded ? Ok(project.Data) : HandleResult(project);
        }

        [HttpPatch("{workspaceId:Guid}/{projectId:Guid}")]
        public async Task<IActionResult> UpdateProject([FromRoute] Guid workspaceId, Guid projectId, [FromBody] UpdateProjectDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await projectService.UpdateProjectAsync(workspaceId, projectId, CurrentUserId, dto);
            return HandleResult(response);
        }
    }
}