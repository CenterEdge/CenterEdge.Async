using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

// We use ConfigureAwait(false) in tests explicitly to test the correct behaviors. It isn't used in a way
// that will interfere with test parallelization.
#pragma warning disable xUnit1030

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
            AsyncHelper.RunSync(async () =>
            {
                i += 1;
                await Task.Delay(10);
                i += 1;
                await Task.Delay(10);
                i += 1;
            });

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

            await Task.Delay(500, TestContext.Current.CancellationToken);
            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSync_Task_ConfigureAwaitFalse_DoesAllTasks()
        {
            // Arrange

            var i = 0;

            // Act
            AsyncHelper.RunSync(async () =>
            {
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
            });

            // Assert

            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSync_Task_ExceptionAfterAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync(async () =>
                {
                    await Task.Delay(10);

                    throw new InvalidOperationException();
                }));
        }

        [Fact]
        public void RunSync_Task_ExceptionBeforeAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync(() => throw new InvalidOperationException()));
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
                AsyncHelper.RunSync(() => throw new InvalidOperationException());
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
            AsyncHelper.RunSync(async () =>
            {
                await Task.Yield();

#pragma warning disable 4014
                DelayedActionAsync(TimeSpan.FromMilliseconds(400), () => called = true);
#pragma warning restore 4014
            });

            // Assert

            Assert.False(called);

            Thread.Sleep(500);

            Assert.True(called);

            mockSync.Verify(
                m => m.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()),
                Times.Once);
        }

        #endregion

        #region RunSyncWithState_Task

        [Fact]
        public void RunSyncWithState_Task_DoesAllTasks()
        {
            // Arrange

            var i = 0;

            // Act
            AsyncHelper.RunSync(async _ =>
            {
                i += 1;
                await Task.Delay(10);
                i += 1;
                await Task.Delay(10);
                i += 1;
            }, 1);

            // Assert

            Assert.Equal(3, i);
        }

        [Fact]
        public async Task RunSyncWithState_StartsTasksAndCompletesSynchronously_DoesAllTasks()
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
            AsyncHelper.RunSync(state =>
            {
#pragma warning disable CS4014
                for (var j = 0; j < 3; j++)
                {
                    var _ = IncrementAsync();
                }
#pragma warning restore CS4014

                return Task.CompletedTask;
            }, 1);

            // Assert

            await Task.Delay(500, TestContext.Current.CancellationToken);
            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSyncWithState_Task_ConfigureAwaitFalse_DoesAllTasks()
        {
            // Arrange

            var i = 0;

            // Act
            AsyncHelper.RunSync(async _ =>
            {
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
            }, 1);

            // Assert

            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSyncWithState_Task_ExceptionAfterAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync(async _ =>
                {
                    await Task.Delay(10);

                    throw new InvalidOperationException();
                }, 1));
        }

        [Fact]
        public void RunSyncWithState_Task_ExceptionBeforeAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync(_ => throw new InvalidOperationException(), 1));
        }

        [Fact]
        public void RunSyncWithState_Task_ThrowsException_ResetsSyncContext()
        {
            // Arrange

            var sync = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(sync);

            // Act
            try
            {
                AsyncHelper.RunSync(_ => throw new InvalidOperationException(), 1);
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            // Assert

            Assert.Equal(sync, SynchronizationContext.Current);
        }

        [Fact]
        public void RunSyncWithState_Task_DanglingContinuations_HandledOnParentSyncContext()
        {
            // Arrange

            var mockSync = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(mockSync.Object);

            var called = false;

            // Act
            AsyncHelper.RunSync(async _ =>
            {
                await Task.Yield();

#pragma warning disable 4014
                DelayedActionAsync(TimeSpan.FromMilliseconds(400), () => called = true);
#pragma warning restore 4014
            }, 1);

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
            AsyncHelper.RunSync(async ValueTask () =>
            {
                i += 1;
                await Task.Delay(10);
                i += 1;
                await Task.Delay(10);
                i += 1;
            });

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

            await Task.Delay(500, TestContext.Current.CancellationToken);
            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSync_ValueTask_ConfigureAwaitFalse_DoesAllTasks()
        {
            // Arrange

            var i = 0;

            // Act
            AsyncHelper.RunSync(async ValueTask () =>
            {
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
            });

            // Assert

            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSync_ValueTask_ExceptionAfterAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync(async ValueTask () =>
                {
                    await Task.Delay(10);

                    throw new InvalidOperationException();
                }));
        }

        [Fact]
        public void RunSync_ValueTask_ExceptionBeforeAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync(ValueTask () => throw new InvalidOperationException()));
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
                AsyncHelper.RunSync(ValueTask () => throw new InvalidOperationException());
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
            AsyncHelper.RunSync(async ValueTask () =>
            {
                await Task.Yield();

#pragma warning disable 4014
                DelayedActionAsync(TimeSpan.FromMilliseconds(400), () => called = true);
#pragma warning restore 4014
            });

            // Assert

            Assert.False(called);

            Thread.Sleep(500);

            Assert.True(called);

            mockSync.Verify(
                m => m.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()),
                Times.Once);
        }

        #endregion

        #region RunSyncWithState_ValueTask

        [Fact]
        public void RunSyncWithState_ValueTask_DoesAllTasks()
        {
            // Arrange

            var i = 0;

            // Act
            AsyncHelper.RunSync(async ValueTask (state) =>
            {
                i += 1;
                await Task.Delay(10);
                i += 1;
                await Task.Delay(10);
                i += 1;
            }, 1);

            // Assert

            Assert.Equal(3, i);
        }

        [Fact]
        public async Task RunSyncWithState_ValueTask_StartsTasksAndCompletesSynchronously_DoesAllTasks()
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
            AsyncHelper.RunSync(state =>
            {
#pragma warning disable CS4014
                for (var j = 0; j < 3; j++)
                {
                    var _ = IncrementAsync();
                }
#pragma warning restore CS4014

                return new ValueTask();
            }, 1);

            // Assert

            await Task.Delay(500, TestContext.Current.CancellationToken);
            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSyncWithState_ValueTask_ConfigureAwaitFalse_DoesAllTasks()
        {
            // Arrange

            var i = 0;

            // Act
            AsyncHelper.RunSync(async ValueTask (state) =>
            {
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
            }, 1);

            // Assert

            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSyncWithState_ValueTask_ExceptionAfterAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync(async ValueTask (state) =>
                {
                    await Task.Delay(10);

                    throw new InvalidOperationException();
                }, 1));
        }

        [Fact]
        public void RunSyncWithState_ValueTask_ExceptionBeforeAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync(ValueTask (_) => throw new InvalidOperationException(), 1));
        }

        [Fact]
        public void RunSyncWithState_ValueTask_ThrowsException_ResetsSyncContext()
        {
            // Arrange

            var sync = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(sync);

            // Act
            try
            {
                AsyncHelper.RunSync(ValueTask (_) => throw new InvalidOperationException(), 1);
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            // Assert

            Assert.Equal(sync, SynchronizationContext.Current);
        }

        [Fact]
        public void RunSyncWithState_ValueTask_DanglingContinuations_HandledOnParentSyncContext()
        {
            // Arrange

            var mockSync = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(mockSync.Object);

            var called = false;

            // Act
            AsyncHelper.RunSync(async ValueTask (state) =>
            {
                await Task.Yield();

#pragma warning disable 4014
                DelayedActionAsync(TimeSpan.FromMilliseconds(400), () => called = true);
#pragma warning restore 4014
            }, 1);

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
            var result = AsyncHelper.RunSync(async () =>
            {
                var i = 1;
                await Task.Delay(10);
                i += 1;
                await Task.Delay(10);
                i += 1;
                return i;
            });

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

            await Task.Delay(500, TestContext.Current.CancellationToken);
            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSync_TaskT_ConfigureAwaitFalse_DoesAllTasks()
        {
            // Act
            var result = AsyncHelper.RunSync(async () =>
            {
                var i = 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                return i;
            });

            // Assert

            Assert.Equal(3, result);
        }

        [Fact]
        public void RunSync_TaskT_ExceptionAfterAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync(async () =>
                {
                    await Task.Delay(10);

                    throw new InvalidOperationException();
                }));
        }

        [Fact]
        public void RunSync_TaskT_ExceptionBeforeAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync(() => throw new InvalidOperationException()));
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
                AsyncHelper.RunSync(() => throw new InvalidOperationException());
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
            AsyncHelper.RunSync(async () =>
            {
                await Task.Yield();

#pragma warning disable 4014
                DelayedActionAsync(TimeSpan.FromMilliseconds(400), () => called = true);
#pragma warning restore 4014

                return 0;
            });

            // Assert

            Assert.False(called);

            Thread.Sleep(500);

            Assert.True(called);

            mockSync.Verify(
                m => m.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()),
                Times.Once);
        }

        #endregion

        #region RunSyncWithState_TaskT

        [Fact]
        public void RunSyncWithState_TaskT_DoesAllTasks()
        {
            // Act
            var result = AsyncHelper.RunSync(async state =>
            {
                var i = 1;
                await Task.Delay(10);
                i += 1;
                await Task.Delay(10);
                i += 1;
                return i;
            }, 1);

            // Assert

            Assert.Equal(3, result);
        }

        [Fact]
        public async Task RunSyncWithState_TaskT_StartsTasksAndCompletesSynchronously_DoesAllTasks()
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
            AsyncHelper.RunSync(state =>
            {
#pragma warning disable CS4014
                for (var j = 0; j < 3; j++)
                {
                    var _ = IncrementAsync();
                }
#pragma warning restore CS4014

                return Task.FromResult(true);
            }, 1);

            // Assert

            await Task.Delay(500, TestContext.Current.CancellationToken);
            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSyncWithState_TaskT_ConfigureAwaitFalse_DoesAllTasks()
        {
            // Act
            var result = AsyncHelper.RunSync(async state =>
            {
                var i = 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                return i;
            }, 1);

            // Assert

            Assert.Equal(3, result);
        }

        [Fact]
        public void RunSyncWithState_TaskT_ExceptionAfterAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync(async state =>
                {
                    await Task.Delay(10);

                    throw new InvalidOperationException();
                }, 1));
        }

        [Fact]
        public void RunSyncWithState_TaskT_ExceptionBeforeAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync(_ => throw new InvalidOperationException(), 1));
        }

        [Fact]
        public void RunSyncWithState_TaskT_ThrowsException_ResetsSyncContext()
        {
            // Arrange

            var sync = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(sync);

            // Act
            try
            {
                AsyncHelper.RunSync(_ => throw new InvalidOperationException(), 1);
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            // Assert

            Assert.Equal(sync, SynchronizationContext.Current);
        }

        [Fact]
        public void RunSyncWithState_TaskT_DanglingContinuations_HandledOnParentSyncContext()
        {
            // Arrange

            var mockSync = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(mockSync.Object);

            var called = false;

            // Act
            AsyncHelper.RunSync(async state =>
            {
                await Task.Yield();

#pragma warning disable 4014
                DelayedActionAsync(TimeSpan.FromMilliseconds(400), () => called = true);
#pragma warning restore 4014

                return 0;
            }, 1);

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
            var result = AsyncHelper.RunSync(async ValueTask<int> () =>
            {
                var i = 1;
                await Task.Delay(10);
                i += 1;
                await Task.Delay(10);
                i += 1;
                return i;
            });

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

            await Task.Delay(500, TestContext.Current.CancellationToken);
            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSync_ValueTaskT_ConfigureAwaitFalse_DoesAllTasks()
        {
            // Act
            var result = AsyncHelper.RunSync(async ValueTask<int> () =>
            {
                var i = 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                return i;
            });

            // Assert

            Assert.Equal(3, result);
        }

        [Fact]
        public void RunSync_ValueTaskT_ExceptionAfterAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync(async ValueTask<int> () =>
                {
                    await Task.Delay(10);

                    throw new InvalidOperationException();
                }));
        }

        [Fact]
        public void RunSync_ValueTaskT_ExceptionBeforeAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync(ValueTask<int> () => throw new InvalidOperationException()));
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
                AsyncHelper.RunSync(ValueTask<int> () => throw new InvalidOperationException());
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
            AsyncHelper.RunSync(async ValueTask<int> () =>
            {
                await Task.Yield();

#pragma warning disable 4014
                DelayedActionAsync(TimeSpan.FromMilliseconds(400), () => called = true);
#pragma warning restore 4014

                return 0;
            });

            // Assert

            Assert.False(called);

            Thread.Sleep(500);

            Assert.True(called);

            mockSync.Verify(
                m => m.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()),
                Times.Once);
        }

        #endregion

        #region RunSyncWithState_ValueTaskT

        [Fact]
        public void RunSyncWithState_ValueTaskT_DoesAllTasks()
        {
            // Act
            var result = AsyncHelper.RunSync(async ValueTask<int> (state) =>
            {
                var i = 1;
                await Task.Delay(10);
                i += 1;
                await Task.Delay(10);
                i += 1;
                return i;
            }, 1);

            // Assert

            Assert.Equal(3, result);
        }

        [Fact]
        public async Task RunSyncWithState_ValueTaskT_StartsTasksAndCompletesSynchronously_DoesAllTasks()
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
            AsyncHelper.RunSync(state =>
            {
#pragma warning disable CS4014
                for (var j = 0; j < 3; j++)
                {
                    var _ = IncrementAsync();
                }
#pragma warning restore CS4014

                return new ValueTask<bool>(true);
            }, 1);

            // Assert

            await Task.Delay(500, TestContext.Current.CancellationToken);
            Assert.Equal(3, i);
        }

        [Fact]
        public void RunSyncWithState_ValueTaskT_ConfigureAwaitFalse_DoesAllTasks()
        {
            // Act
            var result = AsyncHelper.RunSync(async ValueTask<int> (state) =>
            {
                var i = 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                await Task.Delay(10).ConfigureAwait(false);
                i += 1;
                return i;
            }, 1);

            // Assert

            Assert.Equal(3, result);
        }

        [Fact]
        public void RunSyncWithState_ValueTaskT_ExceptionAfterAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync(async ValueTask<int> (state) =>
                {
                    await Task.Delay(10);

                    throw new InvalidOperationException();
                }, 1));
        }

        [Fact]
        public void RunSyncWithState_ValueTaskT_ExceptionBeforeAwait_ThrowsException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                AsyncHelper.RunSync(ValueTask<int> (_) => throw new InvalidOperationException(), 1));
        }

        [Fact]
        public void RunSyncWithState_ValueTaskT_ThrowsException_ResetsSyncContext()
        {
            // Arrange

            var sync = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(sync);

            // Act
            try
            {
                AsyncHelper.RunSync(ValueTask<int> (_) => throw new InvalidOperationException(), 1);
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            // Assert

            Assert.Equal(sync, SynchronizationContext.Current);
        }

        [Fact]
        public void RunSyncWithState_ValueTaskT_DanglingContinuations_HandledOnParentSyncContext()
        {
            // Arrange

            var mockSync = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(mockSync.Object);

            var called = false;

            // Act
            AsyncHelper.RunSync(async ValueTask<int> (state) =>
            {
                await Task.Yield();

#pragma warning disable 4014
                DelayedActionAsync(TimeSpan.FromMilliseconds(400), () => called = true);
#pragma warning restore 4014

                return 0;
            }, 1);

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

        private static async Task DelayedActionAsync(TimeSpan delay, Action action)
        {
            await Task.Delay(delay);

            action.Invoke();
        }

        #endregion
    }
}
