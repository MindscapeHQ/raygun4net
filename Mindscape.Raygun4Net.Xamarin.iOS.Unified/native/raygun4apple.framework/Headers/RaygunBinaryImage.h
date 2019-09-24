//
//  RaygunBinaryImage.h
//  raygun4apple
//
//  Created by raygundev on 8/3/18.
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

#ifndef RaygunBinaryImage_h
#define RaygunBinaryImage_h

#import <Foundation/Foundation.h>

@interface RaygunBinaryImage : NSObject

@property (nonatomic, copy) NSNumber *cpuType;
@property (nonatomic, copy) NSNumber *cpuSubtype;
@property (nonatomic, copy) NSNumber *imageAddress;
@property (nonatomic, copy) NSNumber *imageSize;
@property (nonatomic, copy) NSString *name;
@property (nonatomic, copy) NSString *uuid;

- (instancetype)init NS_UNAVAILABLE;

/*
 * Initializes a RaygunBinaryImage
 *
 * @return RaygunBinaryImage
 */
- (instancetype)initWithUuId:(NSString *)uuid
                    withName:(NSString *)name
                 withCpuType:(NSNumber *)cpuType
              withCpuSubType:(NSNumber *)cpuSubType
            withImageAddress:(NSNumber *)imageAddress
               withImageSize:(NSNumber *)imageSize NS_DESIGNATED_INITIALIZER;

/*
 * Creates and returns a dictionary with the binary image properties and their values.
 * Used when constructing the crash report that is sent to Raygun.
 *
 * @return a new Dictionary with the binary image properties and their values.
 */
- (NSDictionary *)convertToDictionary;

@end

#endif /* RaygunBinaryImage_h */
