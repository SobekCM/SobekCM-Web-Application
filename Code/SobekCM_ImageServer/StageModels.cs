using System;

namespace SobekCM.ImageServer
{
    /// <summary> Decrypted contents of a GET /render request's token query parameter. </summary>
    /// <param name="Bucket"> GCS bucket the object lives in </param>
    /// <param name="Tag"> Object key prefix ("folder") for the digital resource, e.g. "SOBEK/AA00008275/00001/" --
    /// matches SobekFileSystem.AssociFilePath on the caller's side </param>
    /// <param name="FileName"> File name to append to <see cref="Tag"/> to form the full object key; left empty
    /// if <see cref="Tag"/> is already the full object key </param>
    /// <param name="CacheMinutes"> Caller-requested cache lifetime; NULL to use the server's own default </param>
    /// <param name="IssuedUtc"> When the caller created this token -- used to reject stale/replayed tokens </param>
    public record StageRequest(string Bucket, string Tag, string FileName, int? CacheMinutes, DateTime IssuedUtc);
}
