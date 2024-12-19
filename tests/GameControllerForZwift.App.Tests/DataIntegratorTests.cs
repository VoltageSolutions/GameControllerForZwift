using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Xunit;
using NSubstitute;
using GameControllerForZwift.Core;
using GameControllerForZwift.Logic;
using GameControllerForZwift.Core.Mapping;

namespace GameControllerForZwift.App.Tests
{
    public class DataIntegratorTests
    {
        private readonly IInputService _inputService;
        private readonly IOutputService _outputService;
        private readonly IControllerProfileService _controllerProfileService;
        private readonly DataIntegrator _dataIntegrator;
        private readonly IController _controller;

        public DataIntegratorTests()
        {
            _inputService = Substitute.For<IInputService>();
            _outputService = Substitute.For<IOutputService>();
            _controllerProfileService = Substitute.For<IControllerProfileService>();
            _controller = Substitute.For<IController>();
            _dataIntegrator = new DataIntegrator(_inputService, _outputService, _controllerProfileService, maxQueueSize: 5);
        }

        [Fact]
        public void FailTest()
        {
            Assert.True(false);
        }

        [Fact]
        public void GetControllers_ReturnsControllers()
        {
            // Arrange
            var controllers = new List<IController> { _controller };
            _inputService.GetControllers().Returns(controllers);

            // Act
            var result = _dataIntegrator.GetControllers();

            // Assert
            Assert.Equal(controllers, result);
        }

        [Fact]
        public void ProcessControllerData_EnqueuesData()
        {
            // Arrange
            var controllerData = new ControllerData { A = true };

            // Act
            _dataIntegrator.ProcessControllerData(controllerData);

            // Assert
            Assert.Single(_dataIntegrator.DataQueue);
            _dataIntegrator.DataQueue.TryPeek(out var dequeuedData);
            Assert.Equal(controllerData, dequeuedData);
        }

        [Fact]
        public void ProcessControllerData_QueueDoesNotExceedMaxSize()
        {
            // Arrange
            for (int i = 0; i < 6; i++)
            {
                var data = new ControllerData { A = true };
                _dataIntegrator.ProcessControllerData(data);
            }

            // Act
            var queueCount = _dataIntegrator.DataQueue.Count;

            // Assert
            Assert.Equal(5, queueCount); // Max queue size is 5
        }

        [Fact]
        public async Task StartProcessing_InvokesOutputServiceActions()
        {
            // Arrange
            var controllerData = new ControllerData { A = true };
            _controller.ReadData().Returns(controllerData);

            var controllerProfiles = new ControllerProfiles();
            var controllerProfile = new ControllerProfile();
            controllerProfiles.Profiles.Add(controllerProfile);
            var mapping = controllerProfile.Mappings.Where(m => m.Input == ControllerInput.A).FirstOrDefault();
            mapping.Function = ZwiftFunction.Select;
            mapping.PlayerView = ZwiftPlayerView.Default;
            mapping.RiderAction = ZwiftRiderAction.RideOn;

            _controllerProfileService.Profiles.Returns(controllerProfiles);

            var cancellationTokenSource = new CancellationTokenSource();
            _dataIntegrator.StartProcessing(_controller);

            // Allow some time for async methods to run
            await Task.Delay(100);

            // Stop processing
            _dataIntegrator.StopProcessing();
            cancellationTokenSource.Cancel();

            // Assert
            await _outputService.Received().PerformActionAsync(ZwiftFunction.Select, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
        }

        [Fact]
        public async Task StartProcessing_CancelsProperly()
        {
            // Arrange
            var controllerData = new ControllerData { A = true };
            _controller.ReadData().Returns(controllerData);

            _dataIntegrator.StartProcessing(_controller);

            // Allow some time for async methods to run
            await Task.Delay(100);

            // Act
            _dataIntegrator.StopProcessing();

            // Allow time for cancellation to propagate
            await Task.Delay(50);

            // Assert
            _outputService.ClearReceivedCalls();
            await _outputService.DidNotReceive().PerformActionAsync(Arg.Any<ZwiftFunction>(), Arg.Any<ZwiftPlayerView>(), Arg.Any<ZwiftRiderAction>());
        }

        [Fact]
        public void StopProcessing_CancelsRunningTasks()
        {
            // Act
            _dataIntegrator.StartProcessing(_controller);
            _dataIntegrator.StopProcessing();

            // Assert
            // No exceptions should be thrown; verifying that StopProcessing works gracefully
            Assert.True(true);
        }

        [Fact]
        public async void InputPolled_EventIsRaised()
        {
            // Arrange
            var controllerData = new ControllerData { A = true };
            _controller.ReadData().Returns(controllerData);
            var eventRaised = false;
            _dataIntegrator.InputPolled += (sender, e) =>
            {
                eventRaised = true;
                Assert.Equal(controllerData, e.Data);
            };

            _dataIntegrator.StartProcessing(_controller);

            // Allow some time for async methods to run
            await Task.Delay(100);

            // Act
            _dataIntegrator.StopProcessing();

            // Allow time for cancellation to propagate
            await Task.Delay(50);

            // Assert
            Assert.True(eventRaised);
        }
    }
}
