using FluentAssertions;
using ViceMCP.ViceBridge.Commands;
using ViceMCP.ViceBridge.Exceptions;
using ViceMCP.ViceBridge.Responses;
using Xunit;

namespace ViceMCP.Tests
{
    public class ExceptionTests
    {
        [Fact]
        public void ResumeOnStoppedTimeoutException_Should_Store_Response()
        {
            // Arrange
            var response = new EmptyViceResponse(2, ErrorCode.OK);
            
            // Act
            var exception = new ResumeOnStoppedTimeoutException(response);

            // Assert
            exception.Response.Should().Be(response);
            exception.InnerException.Should().BeNull();
        }

        [Fact]
        public void ResumeOnStoppedTimeoutException_Should_Store_Custom_Message()
        {
            // Arrange
            var response = new EmptyViceResponse(2, ErrorCode.OK);
            const string customMessage = "Custom timeout message";

            // Act
            var exception = new ResumeOnStoppedTimeoutException(response, customMessage);

            // Assert
            exception.Message.Should().Be(customMessage);
            exception.Response.Should().Be(response);
            exception.InnerException.Should().BeNull();
        }

        [Fact]
        public void ResumeOnStoppedTimeoutException_Should_Store_InnerException()
        {
            // Arrange
            var response = new EmptyViceResponse(2, ErrorCode.OK);
            const string message = "Timeout occurred";
            var innerException = new TimeoutException("Inner timeout");

            // Act
            var exception = new ResumeOnStoppedTimeoutException(response, message, innerException);

            // Assert
            exception.Message.Should().Be(message);
            exception.Response.Should().Be(response);
            exception.InnerException.Should().Be(innerException);
        }

        [Fact]
        public void SocketDisconnectedException_Should_Have_Default_Message()
        {
            // Arrange & Act
            var exception = new SocketDisconnectedException();

            // Assert
            exception.Message.Should().Contain("SocketDisconnectedException");
            exception.InnerException.Should().BeNull();
        }

        [Fact]
        public void SocketDisconnectedException_Should_Store_Custom_Message()
        {
            // Arrange
            const string customMessage = "Socket was disconnected unexpectedly";

            // Act
            var exception = new SocketDisconnectedException(customMessage);

            // Assert
            exception.Message.Should().Be(customMessage);
            exception.InnerException.Should().BeNull();
        }

        [Fact]
        public void SocketDisconnectedException_Should_Store_InnerException()
        {
            // Arrange
            const string message = "Connection lost";
            var innerException = new IOException("Socket error");

            // Act
            var exception = new SocketDisconnectedException(message, innerException);

            // Assert
            exception.Message.Should().Be(message);
            exception.InnerException.Should().Be(innerException);
        }

        [Fact]
        public void Exceptions_Should_Be_Exception_Derivatives()
        {
            // These exceptions derive from Exception/TimeoutException
            var response = new EmptyViceResponse(2, ErrorCode.OK);
            var resumeException = new ResumeOnStoppedTimeoutException(response, "Test");
            var socketException = new SocketDisconnectedException("Test");

            // Basic check that they're Exception derivatives
            resumeException.Should().BeAssignableTo<TimeoutException>();
            socketException.Should().BeAssignableTo<Exception>();
        }
    }
}