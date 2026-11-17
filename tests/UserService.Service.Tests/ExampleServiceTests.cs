using UserService.Domain.DTOs;
using UserService.Domain.Entities;
using UserService.Repository.Interfaces;
using UserService.Service.Services;
using NSubstitute;

namespace UserService.Service.Tests;

public class ExampleServiceTests
{
    private readonly IExampleRepository _repository = Substitute.For<IExampleRepository>();
    private readonly ExampleService _sut;

    public ExampleServiceTests()
    {
        _sut = new ExampleService(_repository);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenEntityDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Example?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_MapsRequestToResponse()
    {
        _repository.AddAsync(Arg.Any<Example>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Example>());

        var result = await _sut.CreateAsync(new ExampleRequest { Name = "Test", Description = "Desc" });

        Assert.Equal("Test", result.Name);
        Assert.Equal("Desc", result.Description);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenEntityDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Example?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<Example>(), Arg.Any<CancellationToken>());
    }
}
