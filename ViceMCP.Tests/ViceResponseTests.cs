using System.Collections.Immutable;
using FluentAssertions;
using ViceMCP.ViceBridge;
using ViceMCP.ViceBridge.Commands;
using ViceMCP.ViceBridge.Responses;
using ViceMCP.ViceBridge.Shared;
using Xunit;

namespace ViceMCP.Tests
{
    public class ViceResponseTests
    {
        [Fact]
        public void AutoStartResponse_Should_Be_Created()
        {
            // Arrange & Act
            var response = new AutoStartResponse(2, ErrorCode.OK);

            // Assert
            response.Should().NotBeNull();
            response.ApiVersion.Should().Be(2);
            response.ErrorCode.Should().Be(ErrorCode.OK);
        }

        [Fact]
        public void JamResponse_Should_Store_PC_Position()
        {
            // Arrange & Act
            var response = new JamResponse(2, ErrorCode.OK, ProgramCounterPosition: 0x1000);

            // Assert
            response.Should().NotBeNull();
            response.ApiVersion.Should().Be(2);
            response.ErrorCode.Should().Be(ErrorCode.OK);
            response.ProgramCounterPosition.Should().Be(0x1000);
        }

        [Fact]
        public void JamResponse_With_Error_Should_Have_Default_PC()
        {
            // Arrange & Act
            var response = new JamResponse(2, ErrorCode.ObjectDoesNotExist, ProgramCounterPosition: default);

            // Assert
            response.ErrorCode.Should().Be(ErrorCode.ObjectDoesNotExist);
            response.ProgramCounterPosition.Should().Be(0);
        }

        [Fact]
        public void StoppedResponse_Should_Store_PC_Position()
        {
            // Arrange & Act
            var response = new StoppedResponse(2, ErrorCode.OK, ProgramCounterPosition: 0x2000);

            // Assert
            response.Should().NotBeNull();
            response.ApiVersion.Should().Be(2);
            response.ErrorCode.Should().Be(ErrorCode.OK);
            response.ProgramCounterPosition.Should().Be(0x2000);
        }

        [Fact]
        public void ResumedResponse_Should_Store_PC_Position()
        {
            // Arrange & Act
            var response = new ResumedResponse(2, ErrorCode.OK, ProgramCounterPosition: 0x3000);

            // Assert
            response.Should().NotBeNull();
            response.ApiVersion.Should().Be(2);
            response.ErrorCode.Should().Be(ErrorCode.OK);
            response.ProgramCounterPosition.Should().Be(0x3000);
        }

        [Fact]
        public void UndumpResponse_Should_Store_PC_Position()
        {
            // Arrange & Act
            var response = new UndumpResponse(2, ErrorCode.OK, ProgramCounterPosition: 0x4000);

            // Assert
            response.Should().NotBeNull();
            response.ApiVersion.Should().Be(2);
            response.ErrorCode.Should().Be(ErrorCode.OK);
            response.ProgramCounterPosition.Should().Be(0x4000);
        }

        [Fact]
        public void ResourceGetResponse_Should_Store_Resource()
        {
            // Arrange
            var stringResource = new StringResource("TestValue");
            
            // Act
            var response = new ResourceGetResponse(2, ErrorCode.OK, stringResource);

            // Assert
            response.Should().NotBeNull();
            response.ApiVersion.Should().Be(2);
            response.ErrorCode.Should().Be(ErrorCode.OK);
            response.Resource.Should().Be(stringResource);
        }

        [Fact]
        public void ResourceGetResponse_With_Error_Should_Have_Null_Resource()
        {
            // Arrange & Act
            var response = new ResourceGetResponse(2, ErrorCode.ObjectDoesNotExist, Resource: default);

            // Assert
            response.ErrorCode.Should().Be(ErrorCode.ObjectDoesNotExist);
            response.Resource.Should().BeNull();
        }

        [Fact]
        public void FullRegisterItem_Should_Store_Properties()
        {
            // Arrange & Act
            var item = new FullRegisterItem(Id: 0, Size: 2, Name: "A");

            // Assert
            item.Id.Should().Be(0);
            item.Size.Should().Be(2);
            item.Name.Should().Be("A");
        }

        [Fact]
        public void RegistersAvailableResponse_Should_Store_Items()
        {
            // Arrange
            var items = new[]
            {
                new FullRegisterItem(0, 2, "A"),
                new FullRegisterItem(1, 2, "X"),
                new FullRegisterItem(2, 2, "Y")
            };

            // Act
            var response = new RegistersAvailableResponse(2, ErrorCode.OK, items.ToImmutableArray());

            // Assert
            response.Should().NotBeNull();
            response.Items.Should().HaveCount(3);
            response.Items[0].Name.Should().Be("A");
            response.Items[1].Name.Should().Be("X");
            response.Items[2].Name.Should().Be("Y");
        }

        [Fact]
        public void RegistersAvailableResponse_With_Error_Should_Have_Empty_Items()
        {
            // Arrange & Act
            var response = new RegistersAvailableResponse(2, ErrorCode.ObjectDoesNotExist, ImmutableArray<FullRegisterItem>.Empty);

            // Assert
            response.ErrorCode.Should().Be(ErrorCode.ObjectDoesNotExist);
            response.Items.Should().BeEmpty();
        }

        [Fact]
        public void MemoryGetResponse_Should_Dispose_Buffer()
        {
            // Arrange
            var buffer = BufferManager.GetBuffer(10);
            var response = new MemoryGetResponse(2, ErrorCode.OK, buffer);

            // Act
            response.Dispose();

            // Assert - buffer should be disposed (no exception when disposed again)
            buffer.Dispose(); // Should not throw
        }

        [Fact]
        public void DisplayGetResponse_Should_Dispose_Buffer()
        {
            // Arrange
            var buffer = BufferManager.GetBuffer(100);
            var response = new DisplayGetResponse(2, ErrorCode.OK, 320, 200, 0, 0, 320, 200, 8, buffer);

            // Act
            response.Dispose();

            // Assert - buffer should be disposed (no exception when disposed again)
            buffer.Dispose(); // Should not throw
        }

        [Fact]
        public void StringResource_Should_Calculate_Length()
        {
            // Arrange & Act
            var resource = new StringResource("Hello");

            // Assert
            resource.Text.Should().Be("Hello");
            resource.Length.Should().Be(7); // 1 (type) + 1 (length) + 5 (text)
        }

        [Fact]
        public void IntegerResource_Should_Calculate_Length()
        {
            // Arrange & Act
            var resource = new IntegerResource(42);

            // Assert
            resource.Value.Should().Be(42);
            resource.Length.Should().Be(6); // 1 (type) + 1 (length) + 4 (int)
        }
    }
}