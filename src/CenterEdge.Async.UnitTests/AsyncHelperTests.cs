using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace CenterEdge.Async.UnitTests
{
    public class AsyncHelperTests
    {
        #region RunSync_Task

        [Fact]
        public void RunSync_Task_DoesAllTasks()
        {
            // Arrange

            var i = 0;

            // Act
            AsyncHelper.RunSync((Func<Task>)(async () =>
            {
                i += 1;
                await Task.Delay(10);
                i += 1;
                await Task.Delay(10);
                i += 1;
            }));

            // Assert

            Assert.Equal(3, i);
        }

        [Fact]
        public async Task RunSync_StartsTasksAndCompletesSynchronously_DoesAllTasks()
        {
            // Replicates the case where continuations are queued but the main task completes synchronously
            // so the work must be removed from the queue

            // Arrange

            var i = 0;

            async Task IncrementAsync()
            {
                await Task.Yield();
                Interlocked.Increment(ref i);
            }

            // Act
            AsyncHelper.RunSync(() =>
            {
#pragma warning disable CS4014
                for (var j = 0; j < 3; j++)
                {
                    var _ = IncrementAsync();
                }
#pragma warning restore CS4014

                return Task.CompletedTask;
            });

            // Assert

            await Task.Delay(500);
            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSync_Task_ConfigureAwaitFalse_DoesAllTasks()
        {
            // Arrange

            var i = 0;

            // Act
            AsyncHelper.RunSync((Func<Task>)(async () =>
            {
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
            }));

            // Assert

            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSync_Task_ExceptionAfterAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync((Func<Task>)(async () =>
                {
                    await Task.Delay(10);

                    throw new InvalidOperationException();
                })));
        }

        [Fact]
        public void RunSync_Task_ExceptionBeforeAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync((Func<Task>)(() => throw new InvalidOperationException())));
        }

        [Fact]
        public void RunSync_Task_ThrowsException_ResetsSyncContext()
        {
            // Arrange

            var sync = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(sync);

            // Act
            try
            {
                AsyncHelper.RunSync((Func<Task>)(() => throw new InvalidOperationException()));
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            // Assert

            Assert.Equal(sync, SynchronizationContext.Current);
        }

        [Fact]
        public void RunSync_Task_DanglingContinuations_HandledOnParentSyncContext()
        {
            // Arrange

            var mockSync = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(mockSync.Object);

            var called = false;

            // Act
            AsyncHelper.RunSync((Func<Task>)(async () =>
            {
                await Task.Yield();

#pragma warning disable 4014
                DelayedActionAsync(TimeSpan.FromMilliseconds(400), () => called = true);
#pragma warning restore 4014
            }));

            // Assert

            Assert.False(called);

            Thread.Sleep(500);

            Assert.True(called);

            mockSync.Verify(
                m => m.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()),
                Times.Once);
        }

        #endregion

        #region RunSync_ValueTask

        [Fact]
        public void RunSync_ValueTask_DoesAllTasks()
        {
            // Arrange

            var i = 0;

            // Act
            AsyncHelper.RunSync((Func<ValueTask>)(async () =>
            {
                i += 1;
                await Task.Delay(10);
                i += 1;
                await Task.Delay(10);
                i += 1;
            }));

            // Assert

            Assert.Equal(3, i);
        }

        [Fact]
        public async Task RunSync_ValueTask_StartsTasksAndCompletesSynchronously_DoesAllTasks()
        {
            // Replicates the case where continuations are queued but the main task completes synchronously
            // so the work must be removed from the queue

            // Arrange

            var i = 0;

            async Task IncrementAsync()
            {
                await Task.Yield();
                Interlocked.Increment(ref i);
            }

            // Act
            AsyncHelper.RunSync(() =>
            {
#pragma warning disable CS4014
                for (var j = 0; j < 3; j++)
                {
                    var _ = IncrementAsync();
                }
#pragma warning restore CS4014

                return new ValueTask();
            });

            // Assert

            await Task.Delay(500);
            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSync_ValueTask_ConfigureAwaitFalse_DoesAllTasks()
        {
            // Arrange

            var i = 0;

            // Act
            AsyncHelper.RunSync((Func<ValueTask>)(async () =>
            {
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
            }));

            // Assert

            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSync_ValueTask_ExceptionAfterAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync((Func<ValueTask>)(async () =>
                {
                    await Task.Delay(10);

                    throw new InvalidOperationException();
                })));
        }

        [Fact]
        public void RunSync_ValueTask_ExceptionBeforeAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync((Func<ValueTask>)(() => throw new InvalidOperationException())));
        }

        [Fact]
        public void RunSync_ValueTask_ThrowsException_ResetsSyncContext()
        {
            // Arrange

            var sync = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(sync);

            // Act
            try
            {
                AsyncHelper.RunSync((Func<ValueTask>)(() => throw new InvalidOperationException()));
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            // Assert

            Assert.Equal(sync, SynchronizationContext.Current);
        }

        [Fact]
        public void RunSync_ValueTask_DanglingContinuations_HandledOnParentSyncContext()
        {
            // Arrange

            var mockSync = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(mockSync.Object);

            var called = false;

            // Act
            AsyncHelper.RunSync((Func<ValueTask>)(async () =>
            {
                await Task.Yield();

#pragma warning disable 4014
                DelayedActionAsync(TimeSpan.FromMilliseconds(400), () => called = true);
#pragma warning restore 4014
            }));

            // Assert

            Assert.False(called);

            Thread.Sleep(500);

            Assert.True(called);

            mockSync.Verify(
                m => m.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()),
                Times.Once);
        }

        #endregion

        #region RunSync_TaskT

        [Fact]
        public void RunSync_TaskT_DoesAllTasks()
        {
            // Act
            var result = AsyncHelper.RunSync((Func<Task<int>>)(async () =>
            {
                var i = 1;
                await Task.Delay(10);
                i += 1;
                await Task.Delay(10);
                i += 1;
                return i;
            }));

            // Assert

            Assert.Equal(3, result);
        }

        [Fact]
        public async Task RunSync_TaskT_StartsTasksAndCompletesSynchronously_DoesAllTasks()
        {
            // Replicates the case where continuations are queued but the main task completes synchronously
            // so the work must be removed from the queue

            // Arrange

            var i = 0;

            async Task IncrementAsync()
            {
                await Task.Yield();
                Interlocked.Increment(ref i);
            }

            // Act
            AsyncHelper.RunSync(() =>
            {
#pragma warning disable CS4014
                for (var j = 0; j < 3; j++)
                {
                    var _ = IncrementAsync();
                }
#pragma warning restore CS4014

                return Task.FromResult(true);
            });

            // Assert

            await Task.Delay(500);
            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSync_TaskT_ConfigureAwaitFalse_DoesAllTasks()
        {
            // Act
            var result = AsyncHelper.RunSync((Func<Task<int>>)(async () =>
            {
                var i = 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                return i;
            }));

            // Assert

            Assert.Equal(3, result);
        }

        [Fact]
        public void RunSync_TaskT_ExceptionAfterAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync((Func<Task<int>>)(async () =>
                {
                    await Task.Delay(10);

                    throw new InvalidOperationException();
                })));
        }

        [Fact]
        public void RunSync_TaskT_ExceptionBeforeAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync((Func<Task<int>>)(() => throw new InvalidOperationException())));
        }

        [Fact]
        public void RunSync_TaskT_ThrowsException_ResetsSyncContext()
        {
            // Arrange

            var sync = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(sync);

            // Act
            try
            {
                AsyncHelper.RunSync((Func<Task<object>>)(() => throw new InvalidOperationException()));
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            // Assert

            Assert.Equal(sync, SynchronizationContext.Current);
        }

        [Fact]
        public void RunSync_TaskT_DanglingContinuations_HandledOnParentSyncContext()
        {
            // Arrange

            var mockSync = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(mockSync.Object);

            var called = false;

            // Act
            AsyncHelper.RunSync((Func<Task<int>>)(async () =>
            {
                await Task.Yield();

#pragma warning disable 4014
                DelayedActionAsync(TimeSpan.FromMilliseconds(400), () => called = true);
#pragma warning restore 4014

                return 0;
            }));

            // Assert

            Assert.False(called);

            Thread.Sleep(500);

            Assert.True(called);

            mockSync.Verify(
                m => m.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()),
                Times.Once);
        }

        #endregion

        #region RunSync_ValueTaskT

        [Fact]
        public void RunSync_ValueTaskT_DoesAllTasks()
        {
            // Act
            var result = AsyncHelper.RunSync((Func<ValueTask<int>>)(async () =>
            {
                var i = 1;
                await Task.Delay(10);
                i += 1;
                await Task.Delay(10);
                i += 1;
                return i;
            }));

            // Assert

            Assert.Equal(3, result);
        }

        [Fact]
        public async Task RunSync_ValueTaskT_StartsTasksAndCompletesSynchronously_DoesAllTasks()
        {
            // Replicates the case where continuations are queued but the main task completes synchronously
            // so the work must be removed from the queue

            // Arrange

            var i = 0;

            async Task IncrementAsync()
            {
                await Task.Yield();
                Interlocked.Increment(ref i);
            }

            // Act
            AsyncHelper.RunSync(() =>
            {
#pragma warning disable CS4014
                for (var j = 0; j < 3; j++)
                {
                    var _ = IncrementAsync();
                }
#pragma warning restore CS4014

                return new ValueTask<bool>(true);
            });

            // Assert

            await Task.Delay(500);
            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSync_ValueTaskT_ConfigureAwaitFalse_DoesAllTasks()
        {
            // Act
            var result = AsyncHelper.RunSync((Func<ValueTask<int>>)(async () =>
            {
                var i = 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                return i;
            }));

            // Assert

            Assert.Equal(3, result);
        }

        [Fact]
        public void RunSync_ValueTaskT_ExceptionAfterAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync((Func<ValueTask<int>>)(async () =>
                {
                    await Task.Delay(10);

                    throw new InvalidOperationException();
                })));
        }

        [Fact]
        public void RunSync_ValueTaskT_ExceptionBeforeAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync((Func<ValueTask<int>>)(() => throw new InvalidOperationException())));
        }

        [Fact]
        public void RunSync_ValueTaskT_ThrowsException_ResetsSyncContext()
        {
            // Arrange

            var sync = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(sync);

            // Act
            try
            {
                AsyncHelper.RunSync((Func<ValueTask<object>>)(() => throw new InvalidOperationException()));
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            // Assert

            Assert.Equal(sync, SynchronizationContext.Current);
        }

        [Fact]
        public void RunSync_ValueTaskT_DanglingContinuations_HandledOnParentSyncContext()
        {
            // Arrange

            var mockSync = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(mockSync.Object);

            var called = false;

            // Act
            AsyncHelper.RunSync((Func<ValueTask<int>>)(async () =>
            {
                await Task.Yield();

#pragma warning disable 4014
                DelayedActionAsync(TimeSpan.FromMilliseconds(400), () => called = true);
#pragma warning restore 4014

                return 0;
            }));

            // Assert

            Assert.False(called);

            Thread.Sleep(500);

            Assert.True(called);

            mockSync.Verify(
                m => m.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()),
                Times.Once);
        }

        #endregion

        #region Helpers

        private static readonly AsyncLocal<int> asyncLocalField = new();

        private async Task DelayedActionAsync(TimeSpan delay, Action action)
        {
            await Task.Delay(delay);

            action.Invoke();
        }

        #endregion
    }
}
