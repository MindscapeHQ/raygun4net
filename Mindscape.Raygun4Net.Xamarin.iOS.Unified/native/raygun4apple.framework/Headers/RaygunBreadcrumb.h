//
//  RaygunBreadcrumb.h
//  raygun4apple
//
//  Created by Mitchell Duncan on 10/10/18.
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

#ifndef RaygunBreadcrumb_h
#define RaygunBreadcrumb_h

#import <Foundation/Foundation.h>
#import "RaygunDefines.h"

@class RaygunBreadcrumb;

NS_ASSUME_NONNULL_BEGIN

typedef void(^RaygunBreadcrumbBlock)(RaygunBreadcrumb *);

@interface RaygunBreadcrumb : NSObject

/*
 * The message you want to record for this breadcrumb (required)
 */
@property (nonatomic, copy) NSString *message;

/*
 * Any value to categorize your messages
 */
@property (nullable, nonatomic, copy) NSString *category;

/*
 * The display level of the message (valid values are Debug, Info, Warning, Error)
 */
@property (nonatomic) enum  RaygunBreadcrumbLevel level;

/*
 * The type of message (valid values are manual only currently)
 */
@property (nonatomic) enum  RaygunBreadcrumbType type;

/*
 * Milliseconds since the Unix Epoch (required)
 */
@property (nonatomic, copy) NSNumber *timestamp;

/*
 * If relevant, a class name from where the breadcrumb was recorded
 */
@property (nullable, nonatomic, copy) NSString *className;

/*
 * If relevant, a method name from where the breadcrumb was recorded
 */
@property (nullable, nonatomic, copy) NSString *methodName;

/*
 * If relevant, a line number from where the breadcrumb was recorded
 */
@property (nullable, nonatomic, copy) NSNumber *lineNumber;

/*
 * Any custom data you want to record about application state when the breadcrumb was recorded
 */
@property (nullable, nonatomic, strong) NSDictionary *customData;

+ (instancetype)breadcrumbWithBlock:(RaygunBreadcrumbBlock)block;

- (instancetype)initWithBlock:(RaygunBreadcrumbBlock)block NS_DESIGNATED_INITIALIZER;

- (instancetype)init NS_UNAVAILABLE;

+ (instancetype)breadcrumbWithInformation:(NSDictionary *)information;

+ (BOOL)validate:(nullable RaygunBreadcrumb *)breadcrumb withError:(NSError **)error;

- (NSDictionary *)convertToDictionary;

@end

NS_ASSUME_NONNULL_END

#endif /* RaygunBreadcrumb_h */
