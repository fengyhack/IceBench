using Ice;
using Bundle;
using System.Threading;
using System.IO;
using System;
using System.Threading.Tasks;

namespace Bundle
{
    public class WorkerImpl : WorkerDisp_
    {
        private readonly MethodInvokedNotify notify;

        private bool userRequestError = false;

        public WorkerImpl(MethodInvokedNotify notify_)
        {
            notify = notify_;
        }

        public void SetErrorFlag()
        {
            userRequestError = true;
        }

        public override OperationResult PerformAction(OperationType operation, int contentSizeMB, Current current = null)
        {
            if (contentSizeMB < 1 || contentSizeMB > 1024)
            {
                contentSizeMB = 2;
            }

            int MSG = 32;
            int SMALL = contentSizeMB * 256 * 1024;
            int BIG = contentSizeMB * 1024 * 1024;
            int LONG_TIME_MS = 1000; //1s
             //int LONG_TIME_MS = 1000 * 60 * 3; //3min

            try
            {
                notify?.Invoke(operation, false);
                if (userRequestError)
                {
                    userRequestError = false;
                    throw new OperationException("user requested");
                }
                switch (operation)
                {
                    case OperationType.ShortMessage:
                        return new OperationResult("ShortMessage", GenerateRandomData("MESSAGE", MSG));
                    case OperationType.SmallFile:
                        return new OperationResult("SmallFile", GenerateRandomData("SMALL", SMALL));
                    case OperationType.BigFile:
                        return new OperationResult("BigFile", GenerateRandomData("BIG", BIG));
                    case OperationType.LongTime:
                        Thread.Sleep(LONG_TIME_MS);
                        return new OperationResult("LongTime", new byte[1]);
                    default:
                        return new OperationResult("default", new byte[1]);
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        public override Task<OperationResult> PerformActionExAsync(OperationType operation, int contentSizeMB, Current current = null)
        {
            if (contentSizeMB < 1 || contentSizeMB > 1024)
            {
                contentSizeMB = 2;
            }

            int DEFAULT = contentSizeMB * 1024 * 1024;
            int LONG_TIME_MS = 1000 * 60 + 500; //60s+500ms

            try
            {
                notify?.Invoke(operation, false);
                if (userRequestError)
                {
                    userRequestError = false;
                    throw new OperationException("user requested");
                }
                switch (operation)
                {
                    case OperationType.LongTime:
                        return Task.Run(() =>
                        {
                            Thread.Sleep(LONG_TIME_MS);
                            return new OperationResult("LongTime", GenerateRandomData("DEFAULT", DEFAULT));
                        });
                    default:
                        return Task.Run(() =>
                        {
                            return new OperationResult("default", GenerateRandomData("DEFAULT", DEFAULT));
                        });
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        public byte[] GenerateRandomData(string key, int size)
        {
            const string DIR = "./source";
            string fileName = $"./source/{key}.bin";

            byte[] bytes;

            if (!Directory.Exists(DIR))
            {
                Directory.CreateDirectory(DIR);
            }

            var gen = false;
            if (!File.Exists(fileName))
            {
                gen = true;
            }
            else
            {
                var fileSize = new FileInfo(fileName).Length;
                if (Math.Abs(fileSize - size) / (double)size > 0.05)
                {
                    gen = true;
                }
            }

            if(gen)
            {
                bytes = new byte[size];
                for (int i = 0; i < size; ++i)
                {
                    bytes[i] = (byte)((i + 100) % 256);
                }
                File.WriteAllBytes(fileName, bytes);
            }
            else
            {
                bytes = File.ReadAllBytes(fileName);
            }

            return bytes;
        }
    }
}
