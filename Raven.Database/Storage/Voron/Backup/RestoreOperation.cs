﻿using System.IO.Compression;
using System.Linq;
using Raven.Database.Config;
using System;
using System.IO;
using Raven.Database.Storage.Voron.Impl;
using Voron;
using Voron.Impl.Backup;

namespace Raven.Database.Storage.Voron.Backup
{
    public class RestoreOperation : BaseRestoreOperation
    {
		public RestoreOperation(string backupLocation, InMemoryRavenConfiguration configuration, Action<string> operationOutputCallback)
            : base(backupLocation,configuration,operationOutputCallback)
		{
		}

        public void Execute()
        {
			ValidateRestorePreconditions(BackupMethods.Filename);

            try
            {
                CopyIndexes();
				CopyIndexDefinitions();

				var backupFilenamePath = BackupFilenamePath(BackupMethods.Filename);

				if (Directory.GetDirectories(backupLocation, "Inc*").Any() == false)
		            BackupMethods.Full.Restore(backupFilenamePath, configuration.DataDirectory);
	            else
				{
		            //note - restore happens when storage environment is not initialized
					using (var env = new StorageEnvironment(StorageEnvironmentOptions.ForPath(configuration.DataDirectory)))
					{
						BackupMethods.Incremental.Restore(env, backupFilenamePath);
					}
				}

            }
            catch (Exception e)
            {
                LogFailureAndRethrow(e);
            }
        }
    }
}
