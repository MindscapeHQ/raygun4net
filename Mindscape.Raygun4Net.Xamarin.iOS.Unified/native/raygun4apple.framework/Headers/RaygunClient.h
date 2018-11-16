//
//  RaygunClient.h
//  raygun4apple
//
//  Created by Mitchell Duncan on 27/08/18.
//  Copyright © 2018 Raygun Limited. All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall remain in place
// in this source code.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

#import <Foundation/Foundation.h>
#import "RaygunDefines.h"

@class RaygunUserInformation, RaygunMessage, RaygunBreadcrumb;

NS_ASSUME_NONNULL_BEGIN

/*
 * Block can be used to modify the crash report before it is sent to Raygun.
 */
typedef BOOL (^RaygunBeforeSendMessage)(RaygunMessage *message);

@interface RaygunClient : NSObject

@property (nonatomic, class) enum RaygunLoggingLevel logLevel;

@property (nonatomic, class, readonly, copy) NSString *apiKey;

@property (nullable, nonatomic, copy) NSString *applicationVersion;

@property (nullable, nonatomic, strong) NSArray *tags;

@property (nullable, nonatomic, strong) NSDictionary<NSString *, id> *customData;

@property (nullable, nonatomic, strong) RaygunUserInformation *userInformation;

@property (nonatomic, copy) RaygunBeforeSendMessage beforeSendMessage;

@property (nonatomic, assign) int maxReportsStoredOnDevice;

@property (nonatomic, readonly, copy) NSArray<RaygunBreadcrumb *> *breadcrumbs;


// Testing Native Exceptions
- (void)crash;

/*
 * Returns the shared Raygun client
 *
 * @warning This method does not create an instance of the client
 *
 * @return The shared client instance or nil if it has not been created yet.
 */
+ (instancetype)sharedInstance;

/*
 * Creates and returns a shared Raygun client with the given API key.
 * If a client has already been created, this method has no effect.
 *
 * @param apiKey The Raygun API key
 *
 * @return The shared client instance with the given API key.
 */
+ (instancetype)sharedInstanceWithApiKey:(NSString *)apiKey
NS_SWIFT_NAME(sharedInstance(apiKey:));

+ (instancetype)new NS_UNAVAILABLE;

- (instancetype)init NS_UNAVAILABLE;

- (instancetype)initWithApiKey:(NSString *)apiKey NS_DESIGNATED_INITIALIZER;

/*
 * Initializes the client for the automatic and manual sending of crash reports to Raygun.
 */
- (void)enableCrashReporting;

/*
 * Manually send an exception to Raygun with the current state of execution.
 *
 * @warning stack traces will only be populated if you have caught the exception
 *
 * @param exception The exception instance to report upon
 */
- (void)sendException:(NSException *)exception
NS_SWIFT_NAME(send(exception:));

/*
 * Manually send an exception to Raygun with the current state of execution and a list of tags.
 *
 * @warning stack traces will only be populated if you have caught the exception
 *
 * @param exception The exception instance to report upon
 * @param tags A list of tags to be included only with this report
 */
- (void)sendException:(NSException *)exception
             withTags:(nullable NSArray *)tags
NS_SWIFT_NAME(send(exception:tags:));

/*
 * Manually send an exception to Raygun with the current state of execution, a list of tags and a dictionary of custom data.
 *
 * @warning stack traces will only be populated if you have caught the exception
 *
 * @param exception The exception instance to report upon
 * @param tags A list of tags to be included only with this report
 * @param customData A dictionary of information to be included only with this report
 */
- (void)sendException:(NSException *)exception
             withTags:(nullable NSArray *)tags
       withCustomData:(nullable NSDictionary *)customData
NS_SWIFT_NAME(send(exception:tags:customData:));

/*
 * Manually send an exception name and reason to Raygun with the current state of execution, a list of tags and a dictionary of custom data.
 *
 * @param exceptionName Represents the class name of the error in the crash report
 * @param reason Represents the error message in the crash report
 * @param tags A list of tags to be included only with this report
 * @param customData A dictionary of information to be included only with this report
 */
- (void)sendException:(NSString *)exceptionName
           withReason:(nullable NSString *)reason
             withTags:(nullable NSArray *)tags
       withCustomData:(nullable NSDictionary *)customData
NS_SWIFT_NAME(send(exceptionName:reason:tags:customData:));

/*
 * Manually send an error to Raygun with the current state of execution, a list of tags and a dictionary of custom data.
 *
 * @param error The error instance to report upon
 * @param tags A list of tags to be included only with this report
 * @param customData A dictionary of information to be included only with this report
 */
- (void)sendError:(NSError *)error
         withTags:(nullable NSArray *)tags
   withCustomData:(nullable NSDictionary *)customData
NS_SWIFT_NAME(send(error:tags:customData:));

/*
 * Manually send a crash report to Raygun.
 *
 * @param message The crash report to be sent
 */
- (void)sendMessage:(RaygunMessage *)message
NS_SWIFT_NAME(send(message:));

/*
 * Manually record a breadcrumb that will be included in the next crash report.
 *
 * @param breadcrumb The breadcrumb to be included
 */
- (void)recordBreadcrumb:(RaygunBreadcrumb *)breadcrumb
NS_SWIFT_NAME(record(breadcrumb:));

/*
 * Manually record a breadcrumb that will be included in the next crash report.
 *
 * @param message The message you want to record for this breadcrumb (required)
 * @param category Any value to categorize your messages
 * @param level The display level of the message
 * @param customData Any information you want to record about application state
 */
- (void)recordBreadcrumbWithMessage:(NSString *)message
                       withCategory:(nullable NSString *)category
                          withLevel:(enum RaygunBreadcrumbLevel)level
                     withCustomData:(nullable NSDictionary *)customData
NS_SWIFT_NAME(recordBreadcrumb(message:category:level:customData:));

/*
 * Remove all current breadcrumbs.
 */
- (void)clearBreadcrumbs;

// Real User Monitoring (RUM)

/*
 * Initializes the client for the automatic and manual sending of Real User Monitoring (RUM) events to Raygun.
 */
- (void)enableRealUserMonitoring;

/*
 * Initializes the client to automatically monitor network requests and send timing events based upon their duration.
 */
- (void)enableNetworkPerformanceMonitoring;

/*
 * Do not send (ViewLoaded) timing events that match a certain view name.
 */
- (void)ignoreViews:(NSArray *)viewNames;

/*
 * Do not send (NetworkCall) timing events that match a certain url name.
 */
- (void)ignoreURLs:(NSArray *)urls;

/*
 * Manually send a RUM timing event to Raygun.
 *
 * @param type The event timing type
 * @param name The name to represent the event
 * @param milliseconds The time it took for the event in milliseconds
 */
- (void)sendTimingEvent:(enum RaygunEventTimingType)type
               withName:(NSString *)name
           withDuration:(int)duration
NS_SWIFT_NAME(sendTimingEvent(type:name:duration:));

@end

NS_ASSUME_NONNULL_END
