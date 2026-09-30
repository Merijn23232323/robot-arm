using System.Net;
using System.Net.Http.Json;
using RobotArm.Api.Models;

namespace RobotArm.Api.Tests;

public sealed class ApiIntegrationTests : IClassFixture<RobotArmApiFactory>
{
    private readonly HttpClient _client;
    private readonly int _robotId;

    public ApiIntegrationTests(RobotArmApiFactory factory)
    {
        _robotId = factory.SeedRobot();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Root_serves_robot_control_ui()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Robot Arm", content);
        Assert.Contains("send-all", content);
    }

    [Fact]
    public async Task Robot_status_returns_online()
    {
        var status = await _client.GetFromJsonAsync<RobotStatus>("/api/robot/status");

        Assert.NotNull(status);
        Assert.Equal("online", status.Status);
    }

    [Fact]
    public async Task Create_command_is_persisted_and_returned_by_api()
    {
        var command = new RobotCommand
        {
            RobotId = _robotId,
            Servo = "Base",
            Angle = 90
        };

        var createResponse = await _client.PostAsJsonAsync("/api/commands", command);
        var createdCommand = await createResponse.Content.ReadFromJsonAsync<RobotCommand>();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createdCommand);
        Assert.Equal("Base", createdCommand.Servo);
        Assert.Equal(90, createdCommand.Angle);

        var commands = await _client.GetFromJsonAsync<List<RobotCommand>>("/api/commands");

        Assert.NotNull(commands);
        Assert.Contains(commands, item => item.Id == createdCommand.Id);
    }

    private sealed record RobotStatus(string Status, string Message);
}