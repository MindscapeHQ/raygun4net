//
//  RaygunFrame.h
//  raygun4apple
//
//  Created by raygundev on 8/2/18.
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

#ifndef RaygunFrame_h
#define RaygunFrame_h

#import <Foundation/Foundation.h>

@interface RaygunFrame : NSObject

@property (nonatomic, copy) NSNumber *symbolAddress;
@property (nonatomic, copy) NSNumber *instructionAddress;
@property (nonatomic, copy) NSString *symbolName;

- (instancetype)init;

/*
 * Creates and returns a dictionary with the frame properties and their values.
 * Used when constructing the crash report that is sent to Raygun.
 *
 * @return a new Dictionary with the frame properties and their values.
 */
- (NSDictionary *)convertToDictionary;

@end

#endif /* RaygunFrame_h */
