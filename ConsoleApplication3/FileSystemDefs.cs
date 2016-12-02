using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS422
{
    public abstract class Dir422
    {
        public abstract string Name { get; }

        public abstract IList<Dir422> GetDirs();

        public abstract IList<File422> GetFiles();

        public abstract Dir422 Parent { get; }

        public abstract bool ContainsFile(string fileName, bool recursive);

        public abstract bool ContainsDir(string dirName, bool recursive);

        public abstract Dir422 GetDir(string name);

        public abstract File422 GetFile(string name);

        public abstract File422 CreateFile(string name);

        public abstract Dir422 CreateDir(string name);
    }

    public abstract class File422
    {
        public abstract string Name { get; }

        public abstract Dir422 Parent { get; }

        // This stream BETTER NOT support writing at all.
        public abstract Stream OpenReadOnly();

        public abstract Stream OpenReadWrite();

    }

    public abstract class FileSys422
    {
        public abstract Dir422 GetRoot();

        public virtual bool Contains(File422 file)
        {
            return Contains(file.Parent);
        }

        public virtual bool Contains(Dir422 dir)
        {
            if (dir == null)
            {
                return false;
            }

            if (dir == GetRoot())
            {
                return true;
            }

            return Contains(dir.Parent);
        }
    }

    public class StdFSDir : Dir422
    {
        private string m_path;
        private string m_name;
        private StdFSDir m_parent;

        public StdFSDir(string path, StdFSDir parent)
        {
            m_path = path;
            m_parent = parent;
            m_name = findName(m_path);
        }

        public override string Name
        {
            get
            {
                return m_name;
            }
        }

        public override Dir422 Parent
        {
            get
            {
                return m_parent;
            }
        }

        public override bool ContainsDir(string dirName, bool recursive)
        {
            if (dirName.Contains('/') || dirName.Contains('\\'))
            {
                return false;
            }

            foreach (string dir in Directory.GetDirectories(m_path))
            {
                if (findName(dir) == dirName)
                {
                    return true;
                }
            }

            if (recursive)
            {
                foreach (string dir in Directory.GetDirectories(m_path))
                {
                    bool result = GetDir(Path.GetDirectoryName(dir)).ContainsDir(dirName, true);
                    if (result)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override bool ContainsFile(string fileName, bool recursive)
        {
            if (fileName.Contains('/') || fileName.Contains('\\'))
            {
                return false;
            }

            foreach (string file in Directory.GetFiles(m_path))
            {
                if (Path.GetFileName(file) == fileName)
                {
                    return true;
                }
            }

            if (recursive)
            {
                foreach (string dir in Directory.GetDirectories(m_path))
                {
                    return GetDir(Path.GetDirectoryName(dir)).ContainsDir(fileName, true);
                }
            }

            return false;
        }

        public override Dir422 CreateDir(string name)
        {
            // Make sure it is a valid name
            if (name.Contains('/') || name.Contains('\\') ||
                name == null || name == "")
            {
                return null;
            }

            string path = Path.Combine(m_path, name);
            StdFSDir newDir = new StdFSDir(path, this);

            // We need to check if the 
            try
            {
                // If the directory does not already exist, create it.
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                // Will either return the new directory, or the directory
                // that already existed there.
                return newDir;
            }
            catch
            {
                return null;
            }
        }

        public override File422 CreateFile(string name)
        {
            // Check for a valid name
            if (name.Contains('/') || name.Contains('\\') ||
                  name == null || name == "")
            {
                return null;
            }

            // Save the path of the new file
            string path = Path.Combine(m_path, name);
            StdFSFile newFile = new StdFSFile(path, this);

            // Try to create a new file. If it works, return the file object
            // otherwise return null
            try
            {
                // If the file already exists "truncate" the file to 0.
                // Instead of deleting the file I could just write it to
                /// 0, but this seemed like an easier fix.
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                // Create a new file object
                File.Create(Path.Combine(m_path, name));

                return newFile;
            }
            catch
            {
                return null;
            }
        }

        public override Dir422 GetDir(string name)
        {
            if (name.Contains('/') || name.Contains('\\'))
            {
                return null;
            }

            if (this.ContainsDir(name, false))
            {
                return new StdFSDir(Path.Combine(m_path, name), this);
            }

            return null;
        }

        public override IList<Dir422> GetDirs()
        {
            List<Dir422> dirs = new List<Dir422>();
            foreach (string dir in Directory.GetDirectories(m_path))
            {
                dirs.Add(new StdFSDir(dir, this));
            }

            return dirs;
        }

        public override File422 GetFile(string name)
        {
            if (name.Contains('/') || name.Contains('\\'))
            {
                return null;
            }

            if (this.ContainsFile(name, false))
            {
                return new StdFSFile(Path.Combine(m_path, name), this);
            }

            return null;
        }

        public override IList<File422> GetFiles()
        {
            List<File422> files = new List<File422>();

            // Go through each file in the directory of the given path
            foreach (string file in Directory.GetFiles(m_path))
            {
                // Add a new StdFSFile with the path of the file, and the parent directory.
                // parent directory = current directories path.
                files.Add(new StdFSFile(file, this));
            }

            return files;
        }

        private string findName(string path)
        {
            string subPathBuilder = "";
            for (int i = path.Length - 1; i >= 0; i--)
            {
                if (path[i] == '/' || path[i] == '\\')
                {
                    return subPathBuilder.ToString();
                }

                subPathBuilder = path[i] + subPathBuilder;
            }

            return path;
        }
    }

    public class StdFSFile : File422
    {
        private string m_path;
        private StdFSDir m_parent;

        public StdFSFile(string path, Dir422 parent)
        {
            m_path = path;
            m_parent = (StdFSDir)parent;
        }

        public override string Name
        {
            get
            {
                //Grabs the name after the last /
                return Path.GetFileName(m_path);
            }
        }

        public override Dir422 Parent
        {
            get
            {
                return m_parent;
            }
        }

        public override Stream OpenReadOnly()
        {
            try
            {
                FileStream fs = new FileStream(m_path, FileMode.Open, FileAccess.Read);
                return fs;
            }
            catch
            {
                return null;
            }
        }

        public override Stream OpenReadWrite()
        {
            try
            {
                FileStream fs = new FileStream(m_path, FileMode.Open, FileAccess.ReadWrite);
                return fs;
            }
            catch
            {
                return null;
            }
        }
    }

    public class StandardFileSystem : FileSys422
    {
        static StdFSDir m_root;

        public StandardFileSystem()
        {
            m_root = null;
        }

        public static StandardFileSystem Create(string rootDir)
        {
            try
            {
                // Make sure the directory they want to make the root of the FS
                // actually exists before we do anything.
                if (!Directory.Exists(rootDir))
                {
                    return null;
                }

                StandardFileSystem fileSystem = new StandardFileSystem();
                m_root = new StdFSDir(rootDir, null);
                return fileSystem;
            }
            catch
            {
                return null;
            }
        }

        public override Dir422 GetRoot()
        {
            return m_root;
        }
    }





















    public class MemoryFileSystem : FileSys422
    {
        MemFSDir m_root;
        ConcurrentBag<Stream> m_openStreams;

        public MemoryFileSystem()
        {
            m_root = new MemFSDir("/", null);
            m_openStreams = new ConcurrentBag<Stream>();
        }

        public override Dir422 GetRoot()
        {
            return m_root;
        }
    }

    public class MemFSDir : Dir422
    {
        string m_path;
        string m_name;
        MemFSDir m_parent;
        List<Dir422> m_directories;
        List<File422> m_files;

        public MemFSDir(string path, MemFSDir parent)
        {
            m_path = path;
            

            if (m_path == "/")
            {
                m_parent = null;
                m_name = "/";
            }
            else
            {
                m_parent = parent;
                m_name = findName(path);
            }

            m_directories = new List<Dir422>();
            m_files = new List<File422>();
        }

        public override string Name
        {
            get
            {
                return m_name;
            }
        }

        public override Dir422 Parent
        {
            get
            {
                return m_parent;
            }
        }

        public override bool ContainsDir(string dirName, bool recursive)
        {
            foreach (Dir422 dir in m_directories)
            {
                if (dir.Name == dirName)
                {
                    return true;
                }
            }

            if (recursive)
            {
                foreach (Dir422 dir in m_directories)
                {
                    if (dir.ContainsDir(dirName, recursive))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override bool ContainsFile(string fileName, bool recursive)
        {
            foreach (File422 file in m_files)
            {
                if (file.Name == fileName)
                {
                    return true;
                }
            }

            if (recursive)
            {
                foreach (Dir422 dir in m_directories)
                {
                    if (dir.ContainsDir(fileName, recursive))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override Dir422 CreateDir(string name)
        {
            MemFSDir directory = new MemFSDir(Path.Combine(m_path, name), this);
            m_directories.Add(directory);
            return directory;
        }

        public override File422 CreateFile(string name)
        {
            MemFSFile file = new MemFSFile(Path.Combine(m_path, name), this);
            m_files.Add(file);
            return file;
        }

        public override Dir422 GetDir(string name)
        {
            if (this.ContainsDir(name, false))
            {
                return new MemFSDir(Path.Combine(m_path, name), this);
            }

            return null;
        }

        public override IList<Dir422> GetDirs()
        {
            return m_directories;
        }

        public override File422 GetFile(string name)
        {
            if (this.ContainsFile(name, false))
            {
                return new MemFSFile(Path.Combine(m_path, name), this);
            }

            return null;
        }

        public override IList<File422> GetFiles()
        {
            return m_files;
        }

        private string findName(string path)
        {
            string subPathBuilder = "";
            for (int i = path.Length - 1; i >= 0; i--)
            {
                if (path[i] == '/' || path[i] == '\\')
                {
                    return subPathBuilder.ToString();
                }

                subPathBuilder = path[i] + subPathBuilder;
            }

            return path;
        }
    }

    public class MemFSFile : File422
    {
        private string m_path;
        private string m_name;
        private MemFSDir m_parent;
        private string m_contents;
        ConcurrentBag<Stream> m_openStreams;

        public MemFSFile(string path, MemFSDir parent)
        {
            m_path = path;
            m_name = findName(path);
            m_parent = parent;
            m_contents = "";
        }

        public override string Name
        {
            get
            {
                return m_name;
            }
        }

        public override Dir422 Parent
        {
            get
            {
                return m_parent;
            }
        }

        public override Stream OpenReadOnly()
        {
            MyStream myStream = new MyStream(new MemoryStream(), false);
            return myStream;
        }

        public override Stream OpenReadWrite()
        {
            MyStream myStream = new MyStream(new MemoryStream(), true);
            return myStream;
        }

        private string findName(string path)
        {
            string subPathBuilder = "";
            for (int i = path.Length - 1; i >= 0; i--)
            {
                if (path[i] == '/' || path[i] == '\\')
                {
                    return subPathBuilder.ToString();
                }

                subPathBuilder = path[i] + subPathBuilder;
            }

            return path;
        }
    }


    /*
     *      A class I made to wrap itself around a stream. Let's me modify if the stream
     *      is writable or not.
     */
    public class MyStream : Stream
    {
        private MemoryStream m_stream;
        private bool m_canWrite;

        public MyStream(MemoryStream stream, bool canWrite)
        {
            m_stream = stream;
            m_canWrite = canWrite;
        }

        public override bool CanRead
        {
            get
            {
                return m_stream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return m_stream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                if (m_canWrite && m_stream.CanWrite)
                {
                    return true;
                }

                return false;
            }
        }

        public override long Length
        {
            get
            {
                return m_stream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return m_stream.Position;
            }

            set
            {
                m_stream.Position = value;
            }
        }

        public override void Flush()
        {
            m_stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.CanRead)
            {
                return m_stream.Read(buffer, offset, count);
            }

            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (this.CanSeek)
            {
                return m_stream.Seek(offset, origin);
            }

            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            m_stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.CanWrite)
            {
                m_stream.Write(buffer, offset, count);
                return;
            }

            throw new NotSupportedException();
        }
    }
}

