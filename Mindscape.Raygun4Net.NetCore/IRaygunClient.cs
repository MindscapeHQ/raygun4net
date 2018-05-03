using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net
{
  public interface IRaygunClient
  {
    /// <summary>
    /// Gets or sets the user identity string.
    /// </summary>
    string User { get; set; }
    
    /// <summary>
    /// Gets or sets information about the user including the identity string.
    /// </summary>
    RaygunIdentifierMessage UserInfo { get; set; }
    
    /// <summary>
    /// Gets or sets a custom application version identifier for all error messages sent to the Raygun endpoint.
    /// </summary>
    string ApplicationVersion { get; set; }
    
    /// <summary>
    /// Raised just before a message is sent. This can be used to make final adjustments to the <see cref="RaygunMessage"/>, or to cancel the send.
    /// </summary>
    event EventHandler<RaygunSendingMessageEventArgs> SendingMessage;
    
    /// <summary>
    /// Raised before a message is sent. This can be used to add a custom grouping key to a RaygunMessage before sending it to the Raygun service.
    /// </summary>
    event EventHandler<RaygunCustomGroupingKeyEventArgs> CustomGroupingKey;

    void RecordBreadcrumb(string message);
    
    void RecordBreadcrumb(RaygunBreadcrumb crumb);
    
    void ClearBreadcrumbs();

    /// <summary>
    /// Adds a list of outer exceptions that will be stripped, leaving only the valuable inner exception.
    /// This can be used when a wrapper exception, e.g. TargetInvocationException or HttpUnhandledException,
    /// contains the actual exception as the InnerException. The message and stack trace of the inner exception will then
    /// be used by Raygun for grouping and display. The above two do not need to be added manually,
    /// but if you have other wrapper exceptions that you want stripped you can pass them in here.
    /// </summary>
    /// <param name="wrapperExceptions">Exception types that you want removed and replaced with their inner exception.</param>
    void AddWrapperExceptions(params Type[] wrapperExceptions);
    
    /// <summary>
    /// Specifies types of wrapper exceptions that Raygun should send rather than stripping out and sending the inner exception.
    /// This can be used to remove the default wrapper exceptions (TargetInvocationException and HttpUnhandledException).
    /// </summary>
    /// <param name="wrapperExceptions">Exception types that should no longer be stripped away.</param>
    void RemoveWrapperExceptions(params Type[] wrapperExceptions);
    
    /// <summary>
    /// Transmits an exception to Raygun synchronously.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    void Send(Exception exception);
    
    /// <summary>
    /// Transmits an exception to Raygun synchronously specifying a list of string tags associated
    /// with the message for identification.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    void Send(Exception exception, IList<string> tags);
    
    /// <summary>
    /// Transmits an exception to Raygun synchronously specifying a list of string tags associated
    /// with the message for identification, as well as sending a key-value collection of custom data.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    void Send(Exception exception, IList<string> tags, IDictionary userCustomData);

    /// <summary>
    /// Asynchronously transmits a message to Raygun.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    Task SendInBackground(Exception exception);
    
    /// <summary>
    /// Asynchronously transmits an exception to Raygun.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    Task SendInBackground(Exception exception, IList<string> tags);
    
    /// <summary>
    /// Asynchronously transmits an exception to Raygun.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    Task SendInBackground(Exception exception, IList<string> tags, IDictionary userCustomData);
    
    /// <summary>
    /// Asynchronously transmits a message to Raygun.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    Task SendInBackground(RaygunMessage raygunMessage);
    
    /// <summary>
    /// Posts a RaygunMessage to the Raygun api endpoint.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    Task Send(RaygunMessage raygunMessage);
  }
}