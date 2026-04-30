******************************************************************
      * Copyright Amazon.com, Inc. or its affiliates.                   
      * All Rights Reserved.                                            
      *                                                                 
      * Licensed under the Apache License, Version 2.0 (the "License"). 
      * You may not use this file except in compliance with the License.
      * You may obtain a copy of the License at                         
      *                                                                 
      *    http://www.apache.org/licenses/LICENSE-2.0                   
      *                                                                 
      * Unless required by applicable law or agreed to in writing,      
      * software distributed under the License is distributed on an     
      * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND,    
      * either express or implied. See the License for the specific     
      * language governing permissions and limitations under the License
      ****************************************************************** 
      ******************************************************************
      * SECURITY REMEDIATION NOTES:                                     
      * - SEC-USR-PWD-HASH replaces plaintext password storage          
      * - Password hash field increased to 64 chars (SHA-256 hex)       
      * - Added password lifecycle management fields                    
      * - PRODUCTION DEPLOYMENT REQUIRES:                               
      *   1. Integration with RACF/ACF2/Top Secret for authentication   
      *   2. Migration of existing USRSEC records to hashed passwords   
      *   3. Update all programs referencing SEC-USR-PWD to use ESM     
      *   4. Implement password expiration enforcement logic            
      ******************************************************************
       01 SEC-USER-DATA.
         05 SEC-USR-ID                 PIC X(08).
         05 SEC-USR-FNAME              PIC X(20).
         05 SEC-USR-LNAME              PIC X(20).
      *  05 SEC-USR-PWD                PIC X(08).
      *  ** DEPRECATED: Plaintext password - DO NOT USE **
      *  ** Use RACF/ACF2/TSS for authentication instead **
         05 SEC-USR-PWD-HASH           PIC X(64).
      *  ** SHA-256 hash of password (hexadecimal representation) **
         05 SEC-USR-PWD-SALT           PIC X(16).
      *  ** Salt value for password hashing **
         05 SEC-USR-TYPE               PIC X(01).
         05 SEC-USR-PWD-LAST-CHG       PIC X(10).
      *  ** Date of last password change (YYYY-MM-DD) **
         05 SEC-USR-PWD-EXPIRY         PIC X(10).
      *  ** Password expiration date (YYYY-MM-DD) **
         05 SEC-USR-PWD-MUST-CHG       PIC X(01).
      *  ** Y=Must change at next logon, N=Normal **
         05 SEC-USR-FILLER             PIC X(18).
      *
      * Ver: CardDemo_v1.0-15-g27d6c6f-68 Date: 2022-07-19 23:15:59 CDT
      *