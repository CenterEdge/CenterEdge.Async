using System;
using System.Threading;
using System.Threading.Tasks;
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

        #endregion
    }
}
