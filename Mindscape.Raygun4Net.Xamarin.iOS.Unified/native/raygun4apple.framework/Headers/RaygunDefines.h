//
//  RaygunDefines.h
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

#ifndef RaygunDefines_h
#define RaygunDefines_h

#import <Foundation/Foundation.h>

#if TARGET_OS_IOS || TARGET_OS_TV
#define RAYGUN_CAN_USE_UIDEVICE 1
#else
#define RAYGUN_CAN_USE_UIDEVICE 0
#endif

#if RAYGUN_CAN_USE_UIDEVICE
#define RAYGUN_CAN_USE_UIKIT 1
#else
#define RAYGUN_CAN_USE_UIKIT 0
#endif

static NSString *_Nonnull const kRaygunClientVersion = @"1.3.1";

static NSString *_Nonnull const kRaygunIdentifierUserDefaultsKey = @"com.raygun.identifier";
static NSString *_Nonnull const kRaygunSessionLastSeenDefaultsKey = @"com.raygun.session.lastseen";

static NSString *_Nonnull const kApiEndPointForCR  = @"https://api.raygun.com/entries";
static NSString *_Nonnull const kApiEndPointForRUM = @"https://api.raygun.com/events";

static NSString *_Nonnull const kValueNotKnown = @"Unknown";

static double const kSessionExpiryPeriodInSeconds = 30.0 * 60.0; // 30 minutes
static NSInteger const kMaxCrashReportsOnDeviceUpperLimit = 64;
static NSInteger const kMaxRecordedBreadcrumbs = 32;

typedef NS_ENUM(NSInteger, RaygunEventType) {
    RaygunEventTypeSessionStart = 0,
    RaygunEventTypeSessionEnd,
    RaygunEventTypeTiming
};

/*
 * Static internal helper to convert RaygunEventType enum to a string
 */
static NSString *_Nonnull const RaygunEventTypeNames[] = {
    @"session_start",
    @"session_end",
    @"mobile_event_timing"
};

typedef NS_ENUM(NSInteger, RaygunEventTimingType) {
    RaygunEventTimingTypeViewLoaded = 0,
    RaygunEventTimingTypeNetworkCall
};

/*
 * Static internal helper to convert RaygunEventTimingType enum to a string
 */
static NSString *_Nonnull const RaygunEventTimingTypeShortNames[] = {
    @"p",
    @"n"
};

typedef NS_ENUM(NSInteger, RaygunLoggingLevel) {
    RaygunLoggingLevelNone    = 0,
    RaygunLoggingLevelError   = 1,
    RaygunLoggingLevelWarning = 2,
    RaygunLoggingLevelDebug   = 3,
    RaygunLoggingLevelVerbose = 4,
};

/*
 * Static internal helper to convert RaygunLoggingLevel enum to a string
 */
static NSString *_Nonnull const RaygunLoggingLevelNames[] = {
    @"None",
    @"Error",
    @"Warning",
    @"Debug",
    @"Verbose"
};

/*
 * Static internal helper to convert RaygunResponseStatusCode enum to a string
 */
typedef NS_ENUM(NSInteger, RaygunResponseStatusCode) {
    RaygunResponseStatusCodeAccepted      = 202,
    RaygunResponseStatusCodeBadMessage    = 400,
    RaygunResponseStatusCodeInvalidApiKey = 403,
    RaygunResponseStatusCodeLargePayload  = 413,
    RaygunResponseStatusCodeRateLimited   = 429,
};

typedef NS_ENUM(NSInteger, RaygunBreadcrumbType) {
    RaygunBreadcrumbTypeManual = 0,
};

static NSString *_Nonnull const RaygunBreadcrumbTypeNames[] = {
    @"manual"
};

typedef NS_ENUM(NSInteger, RaygunBreadcrumbLevel) {
    RaygunBreadcrumbLevelDebug = 0,
    RaygunBreadcrumbLevelInfo,
    RaygunBreadcrumbLevelWarning,
    RaygunBreadcrumbLevelError
};

static NSString *_Nonnull const RaygunBreadcrumbLevelNames[] = {
    @"debug",
    @"info",
    @"warning",
    @"error"
};

#endif /* RaygunDefines_h */

