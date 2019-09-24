//
//  RaygunUserInformation.h
//  raygun4apple
//
//  Created by Jason Fauchelle on 16/06/15.
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

#ifndef Raygun4Apple_RaygunUserInformation_h
#define Raygun4Apple_RaygunUserInformation_h

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface RaygunUserInformation : NSObject

@property (nonatomic, class, readonly, copy) RaygunUserInformation *anonymousUser;

/*
 * Unique Identifier for this user. Set this to the identifier you use internally to look up users,
 * or a correlation id for anonymous users if you have one. It doesn't have to be unique, but we will
 * treat any duplicated values as the same user. If you use the user's email address as the identifier,
 * enter it here as well as the email field.
 *
 * @warning The identifier must be set in order for any of the other fields to be sent. (required)
 */
@property (nonatomic, strong) NSString *identifier;

/*
 * Device Identifier.
 */
@property (nullable, nonatomic, strong) NSString *uuid;

/*
 * Flag indicating whether a user is anonymous or not.
 * Generally, set this to true if the user is not logged in.
 */
@property (nonatomic) BOOL isAnonymous;

/*
 * User's email address
 */
@property (nullable, nonatomic, strong) NSString *email;

/*
 * User's full name.
 */
@property (nullable, nonatomic, strong) NSString *fullName;

/*
 * User's first or preferred name.
 */
@property (nullable, nonatomic, strong) NSString *firstName;

/*
 * Checks that the required fields are set and the object is not nil.
 */
+ (BOOL)validate:(nullable RaygunUserInformation *)userInformation withError:(NSError **)error;

- (instancetype)init NS_UNAVAILABLE;

/*
 * Creates and returns a RaygunUserInformation object.
 *
 * @param identifier The unique user identifier that you use internally to look up users.
 *
 * @return a new RaygunUserInformation object.
 */
- (instancetype)initWithIdentifier:(NSString *)identifier
NS_SWIFT_NAME(init(identifier:));

/*
 * Creates and returns a RaygunUserInformation object.
 *
 * @param identifier The unique user identifier that you use internally to look up users.
 * @param email The user's email address.
 * @param fullName The user's full name.
 * @param firstName The user's first or preferred name.
 *
 * @return a new RaygunUserInformation object.
 */
- (instancetype)initWithIdentifier:(NSString *)identifier
                         withEmail:(nullable NSString *)email
                      withFullName:(nullable NSString *)fullName
                     withFirstName:(nullable NSString *)firstName
NS_SWIFT_NAME(init(identifier:email:fullName:firstName:));

/*
 * Creates and returns a RaygunUserInformation object.
 *
 * @param identifier The unique user identifier that you use internally to look up users.
 * @param email The user's email address.
 * @param fullName The user's full name.
 * @param firstName The user's first or preferred name.
 * @param isAnonymous True if the user is not logged in, or however you want to define anonymous.
 *
 * @return a new RaygunUserInformation object.
 */
- (instancetype)initWithIdentifier:(NSString *)identifier
                         withEmail:(nullable NSString *)email
                      withFullName:(nullable NSString *)fullName
                     withFirstName:(nullable NSString *)firstName
                   withIsAnonymous:(BOOL) isAnonymous
NS_SWIFT_NAME(init(identifier:email:fullName:firstName:anonymous:));

/*
 * Creates and returns a RaygunUserInformation object.
 *
 * @param identifier The unique user identifier that you use internally to look up users.
 * @param email The user's email address.
 * @param fullName The user's full name.
 * @param firstName The user's first or preferred name.
 * @param isAnonymous True if the user is not logged in, or however you want to define anonymous.
 * @param uuid Device identifier.
 *
 * @return a new RaygunUserInformation object.
 */
- (instancetype)initWithIdentifier:(NSString *)identifier
                         withEmail:(nullable NSString *)email
                      withFullName:(nullable NSString *)fullName
                     withFirstName:(nullable NSString *)firstName
                   withIsAnonymous:(BOOL) isAnonymous
                          withUuid:(nullable NSString *)uuid NS_DESIGNATED_INITIALIZER
NS_SWIFT_NAME(init(identifier:email:fullName:firstName:anonymous:uuid:));

/*
 * Creates and returns a dictionary with the classes properties and their values.
 * Used when constructing the crash report that is sent to Raygun.
 *
 * @return a new Dictionary with the classes properties and their values.
 */
- (NSDictionary *)convertToDictionary;

@end

NS_ASSUME_NONNULL_END

#endif
