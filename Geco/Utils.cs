
namespace Geco;
internal class Utils
{
	internal static async Task RetryAsyncTaskOrThrow<TErrorType>(int retryCount, Func<Task> taskToRun) where TErrorType : Exception
	{
		int counter = 0;
		bool hasError;
		do
		{
			hasError = false;
			try
			{
				if (counter > 0)
					GlobalContext.Logger.Info<Utils>($"Retrying task... (attempt {counter + 1})");

				await taskToRun();
			}
			catch (TErrorType)
			{
				GlobalContext.Logger.Info<Utils>($"Failed executing task. (attempt {counter + 1})");
				hasError = true;
				counter++;
				if (counter >= 3)
					throw;
			}
		}
		while (hasError);
	}
}
