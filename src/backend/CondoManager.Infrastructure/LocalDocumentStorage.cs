using System.Security.Cryptography;
using System.Text.RegularExpressions;
using CondoManager.Application;
using Microsoft.Extensions.Configuration;
namespace CondoManager.Infrastructure;

// No HTTP file provider uses this directory. Every key is generated here, never from a filename.
public sealed class LocalDocumentStorage : IDocumentStorage
{
    readonly string root;
    public LocalDocumentStorage(IConfiguration configuration)
    {
        root=Path.GetFullPath(configuration["Documents:RootPath"]??Path.Combine(AppContext.BaseDirectory,"private-documents"));
        if(root.TrimEnd(Path.DirectorySeparatorChar)==Path.GetPathRoot(root)!.TrimEnd(Path.DirectorySeparatorChar))throw new InvalidOperationException("La radice di un disco non può essere uno storage documentale.");
        if(configuration["Frontend:RootPath"] is string frontend){var publicRoot=Path.GetFullPath(frontend).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;if((root.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar).StartsWith(publicRoot,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Storage privato incompatibile con la directory pubblica frontend.");}
        Directory.CreateDirectory(root);CheckDirectory(root);
        if(!OperatingSystem.IsWindows())File.SetUnixFileMode(root,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
    }
    static void CheckDirectory(string path)
    {
        for(var d=new DirectoryInfo(path);d!=null;d=d.Parent)
            if((d.Attributes&FileAttributes.ReparsePoint)!=0)throw new IOException("Storage documentale: collegamenti simbolici non consentiti.");
    }
    string PathFor(Guid tenant,string key,bool staging=false)
    {
        if(tenant==Guid.Empty||!Regex.IsMatch(key,"\\A[0-9a-f]{32}\\z"))throw new ArgumentException("Chiave storage non valida.");
        var dir=Path.Combine(root,tenant.ToString("N"));Directory.CreateDirectory(dir);CheckDirectory(dir);
        if(!OperatingSystem.IsWindows())File.SetUnixFileMode(dir,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
        var path=Path.Combine(dir,key+(staging?".stage":".bin"));
        if(File.Exists(path)&&(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new IOException("File storage non valido.");
        return path;
    }
    public async Task<StoredFile> Save(Guid tenant,ReadOnlyMemory<byte> bytes,CancellationToken ct)
    {
        var key=Guid.NewGuid().ToString("N");var path=PathFor(tenant,key,true);
        await File.WriteAllTextAsync(PathFor(tenant,key)+".pending",DateTimeOffset.UtcNow.ToString("O"),ct);
        try{await using(var s=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,FileOptions.Asynchronous|FileOptions.WriteThrough)){await s.WriteAsync(bytes,ct);await s.FlushAsync(ct);}
            var actual=await File.ReadAllBytesAsync(path,ct);return new(key,actual.LongLength,Convert.ToHexString(SHA256.HashData(actual)));}
        catch{File.Delete(path);File.Delete(PathFor(tenant,key)+".pending");throw;}
    }
    public Task<string> Commit(Guid tenant,string key,CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();var stage=PathFor(tenant,key,true);var final=PathFor(tenant,key);
        if(!File.Exists(final))File.Move(stage,final,false);
        return Task.FromResult(key);
    }
    public Task DeleteDraft(Guid tenant,string key,CancellationToken ct){ct.ThrowIfCancellationRequested();File.Delete(PathFor(tenant,key,true));return Task.CompletedTask;}
    public Task<bool> Exists(Guid tenant,string key,CancellationToken ct){ct.ThrowIfCancellationRequested();return Task.FromResult(File.Exists(PathFor(tenant,key)));}
    public Task<byte[]> OpenRead(Guid tenant,string key,CancellationToken ct)=>File.ReadAllBytesAsync(PathFor(tenant,key),ct);
    public async Task<StoredFile> GetMetadata(Guid tenant,string key,CancellationToken ct){var b=await OpenRead(tenant,key,ct);return new(key,b.LongLength,Convert.ToHexString(SHA256.HashData(b)));}
    public Task Acknowledge(Guid tenant,string key,CancellationToken ct){ct.ThrowIfCancellationRequested();File.Delete(PathFor(tenant,key)+".pending");return Task.CompletedTask;}
    public Task DiscardUncommitted(Guid tenant,string key,CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();var path=PathFor(tenant,key);
        // A committed/acknowledged file can NEVER be removed through this operation.
        if(File.Exists(path+".pending")){File.Delete(path);File.Delete(PathFor(tenant,key,true));File.Delete(path+".pending");}return Task.CompletedTask;
    }
    public IEnumerable<PendingDocumentFile> Pending()
    {
        CheckDirectory(root);
        foreach(var dir in Directory.EnumerateDirectories(root)){
            if(!Guid.TryParseExact(Path.GetFileName(dir),"N",out var tenant))continue;CheckDirectory(dir);
            foreach(var marker in Directory.EnumerateFiles(dir,"*.bin.pending")){
                var key=Path.GetFileName(marker).Split('.')[0];PathFor(tenant,key);
                if((File.GetAttributes(marker)&FileAttributes.ReparsePoint)!=0)throw new IOException("Marker storage non valido.");
                if(DateTimeOffset.TryParse(File.ReadAllText(marker),out var at))yield return new(tenant,key,at);
            }
        }
    }
}
// Explicit integration seam. This provider makes no claim that a file is malware-free.
public sealed class UnscannedDocumentScanner : IDocumentScanner
{ public Task Inspect(ReadOnlyMemory<byte> bytes,CancellationToken ct){ct.ThrowIfCancellationRequested();return Task.CompletedTask;} }
