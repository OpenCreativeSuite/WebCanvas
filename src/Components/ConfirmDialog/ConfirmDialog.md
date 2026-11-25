# ConfirmDialog

A deliberate confirmation modal for destructive actions such as deleting a page, block, schema or automation handler.

## Integration
It renders the current StudioSession.Confirm request, including danger styling, message and optional cancel button. AcceptConfirm runs the stored action; CloseConfirm dismisses it. The root app mounts it when Confirm is not null.
