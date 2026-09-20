namespace AnonyMeow.Services.ContentSubmission;

// One seam that Post/Comment/DirectMessage creation all evaluate submitted text through.
// "Evaluate" is kept separate from "act on the result" (throw/block) so a future caller (e.g.
// Phase 8's spam checks, which plug into this same seam) can apply different policy to a
// detection than PII's hard-block, without reshaping this interface.
public interface IContentSubmissionPipeline
{
    Task<ContentSubmissionResult> EvaluateAsync(ContentSubmissionRequest submission, CancellationToken cancellationToken = default);
}
