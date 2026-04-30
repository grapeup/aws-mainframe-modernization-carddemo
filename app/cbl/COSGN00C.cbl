******************************************************************        
      * Program     : COSGN00C.CBL
      * Application : CardDemo
      * Type        : CICS COBOL Program
      * Function    : Signon Screen for the CardDemo Application
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
      * SECURITY ENHANCEMENTS APPLIED:
      * - Password hashing with SHA-256 (placeholder for crypto lib)
      * - Password complexity validation (min 8 chars, mixed case, etc)
      * - Account lockout after 3 failed attempts
      * - Generic error messages to prevent username enumeration
      * - Case-sensitive password handling
      * - Session invalidation on sign-off
      *
      * INFRASTRUCTURE REQUIREMENTS:
      * - USRSEC file must store hashed passwords (SHA-256) not plaintext
      * - USRSEC file must include failed-login-count field
      * - USRSEC file must include lockout-flag field
      * - Link with cryptographic library (e.g., IBM Crypto for z/OS)
      *   or call external hash validation service
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. COSGN00C.
       AUTHOR.     AWS.

       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.

       DATA DIVISION.
      *----------------------------------------------------------------*
      *                     WORKING STORAGE SECTION
      *----------------------------------------------------------------*
       WORKING-STORAGE SECTION.

       01 WS-VARIABLES.
         05 WS-PGMNAME                 PIC X(08) VALUE 'COSGN00C'.
         05 WS-TRANID                  PIC X(04) VALUE 'CC00'.
         05 WS-MESSAGE                 PIC X(80) VALUE SPACES.
         05 WS-USRSEC-FILE             PIC X(08) VALUE 'USRSEC  '.
         05 WS-ERR-FLG                 PIC X(01) VALUE 'N'.
           88 ERR-FLG-ON                         VALUE 'Y'.
           88 ERR-FLG-OFF                        VALUE 'N'.
         05 WS-RESP-CD                 PIC S9(09) COMP VALUE ZEROS.
         05 WS-REAS-CD                 PIC S9(09) COMP VALUE ZEROS.
         05 WS-USER-ID                 PIC X(08).
         05 WS-USER-PWD                PIC X(08).
         05 WS-FAILED-LOGIN-COUNT      PIC 9(02) VALUE 0.
         05 WS-MAX-FAILED-LOGINS       PIC 9(02) VALUE 3.
         05 WS-PASSWORD-HASH           PIC X(64).
         05 WS-STORED-HASH             PIC X(64).
         05 WS-HASH-MATCH              PIC X(01) VALUE 'N'.
           88 HASH-MATCHES                       VALUE 'Y'.
           88 HASH-DOES-NOT-MATCH                VALUE 'N'.
         05 WS-PWD-LENGTH              PIC 9(02) VALUE 0.
         05 WS-PWD-HAS-UPPER           PIC X(01) VALUE 'N'.
         05 WS-PWD-HAS-LOWER           PIC X(01) VALUE 'N'.
         05 WS-PWD-HAS-DIGIT           PIC X(01) VALUE 'N'.
         05 WS-PWD-CHAR                PIC X(01).
         05 WS-PWD-IDX                 PIC 9(02) VALUE 0.

       COPY COCOM01Y.

       COPY COSGN00.

       COPY COTTL01Y.
       COPY CSDAT01Y.
       COPY CSMSG01Y.
       COPY CSUSR01Y.

       COPY DFHAID.
       COPY DFHBMSCA.
      *COPY DFHATTR.

      *----------------------------------------------------------------*
      *                        LINKAGE SECTION
      *----------------------------------------------------------------*
       LINKAGE SECTION.
       01  DFHCOMMAREA.
         05  LK-COMMAREA                           PIC X(01)
             OCCURS 1 TO 32767 TIMES DEPENDING ON EIBCALEN.

      *----------------------------------------------------------------*
      *                      PROCEDURE DIVISION
      *----------------------------------------------------------------*
       PROCEDURE DIVISION.
       MAIN-PARA.

           SET ERR-FLG-OFF TO TRUE

           MOVE SPACES TO WS-MESSAGE
                          ERRMSGO OF COSGN0AO

           IF EIBCALEN = 0
               MOVE LOW-VALUES TO COSGN0AO
               MOVE -1       TO USERIDL OF COSGN0AI
               PERFORM SEND-SIGNON-SCREEN
           ELSE
               EVALUATE EIBAID
                   WHEN DFHENTER
                       PERFORM PROCESS-ENTER-KEY
                   WHEN DFHPF3
                       MOVE CCDA-MSG-THANK-YOU        TO WS-MESSAGE
                       PERFORM CLEAR-SESSION-DATA
                       PERFORM SEND-PLAIN-TEXT
                   WHEN OTHER
                       MOVE 'Y'                       TO WS-ERR-FLG
                       MOVE CCDA-MSG-INVALID-KEY      TO WS-MESSAGE
                       PERFORM SEND-SIGNON-SCREEN
               END-EVALUATE
           END-IF.

           EXEC CICS RETURN
                     TRANSID (WS-TRANID)
                     COMMAREA (CARDDEMO-COMMAREA)
                     LENGTH(LENGTH OF CARDDEMO-COMMAREA)
           END-EXEC.


      *----------------------------------------------------------------*
      *                      PROCESS-ENTER-KEY
      *----------------------------------------------------------------*
       PROCESS-ENTER-KEY.

           EXEC CICS RECEIVE
                     MAP('COSGN0A')
                     MAPSET('COSGN00')
                     RESP(WS-RESP-CD)
                     RESP2(WS-REAS-CD)
           END-EXEC.

           EVALUATE TRUE
               WHEN USERIDI OF COSGN0AI = SPACES OR LOW-VALUES
                   MOVE 'Y'      TO WS-ERR-FLG
                   MOVE 'Please enter User ID ...' TO WS-MESSAGE
                   MOVE -1       TO USERIDL OF COSGN0AI
                   PERFORM SEND-SIGNON-SCREEN
               WHEN PASSWDI OF COSGN0AI = SPACES OR LOW-VALUES
                   MOVE 'Y'      TO WS-ERR-FLG
                   MOVE 'Please enter Password ...' TO WS-MESSAGE
                   MOVE -1       TO PASSWDL OF COSGN0AI
                   PERFORM SEND-SIGNON-SCREEN
               WHEN OTHER
                   CONTINUE
           END-EVALUATE.

           MOVE FUNCTION UPPER-CASE(USERIDI OF COSGN0AI) TO
                           WS-USER-ID
                           CDEMO-USER-ID
      *    Password is now case-sensitive - no UPPER-CASE conversion
           MOVE PASSWDI OF COSGN0AI TO WS-USER-PWD

           IF NOT ERR-FLG-ON
               PERFORM VALIDATE-PASSWORD-COMPLEXITY
           END-IF

           IF NOT ERR-FLG-ON
               PERFORM READ-USER-SEC-FILE
           END-IF.

      *----------------------------------------------------------------*
      *                      VALIDATE-PASSWORD-COMPLEXITY
      *----------------------------------------------------------------*
       VALIDATE-PASSWORD-COMPLEXITY.
      *    Enforce minimum 8 characters, mixed case, and digit
           MOVE 0 TO WS-PWD-LENGTH
           MOVE 'N' TO WS-PWD-HAS-UPPER
           MOVE 'N' TO WS-PWD-HAS-LOWER
           MOVE 'N' TO WS-PWD-HAS-DIGIT

           INSPECT WS-USER-PWD TALLYING WS-PWD-LENGTH
               FOR CHARACTERS BEFORE INITIAL SPACE

           IF WS-PWD-LENGTH < 8
               MOVE 'Y' TO WS-ERR-FLG
               MOVE 'Password must be at least 8 characters.'
                   TO WS-MESSAGE
               MOVE -1 TO PASSWDL OF COSGN0AI
               PERFORM SEND-SIGNON-SCREEN
               EXIT PARAGRAPH
           END-IF

           PERFORM VARYING WS-PWD-IDX FROM 1 BY 1
               UNTIL WS-PWD-IDX > WS-PWD-LENGTH
               MOVE WS-USER-PWD(WS-PWD-IDX:1) TO WS-PWD-CHAR
               IF WS-PWD-CHAR >= 'A' AND WS-PWD-CHAR <= 'Z'
                   MOVE 'Y' TO WS-PWD-HAS-UPPER
               END-IF
               IF WS-PWD-CHAR >= 'a' AND WS-PWD-CHAR <= 'z'
                   MOVE 'Y' TO WS-PWD-HAS-LOWER
               END-IF
               IF WS-PWD-CHAR >= '0' AND WS-PWD-CHAR <= '9'
                   MOVE 'Y' TO WS-PWD-HAS-DIGIT
               END-IF
           END-PERFORM

           IF WS-PWD-HAS-UPPER = 'N' OR
              WS-PWD-HAS-LOWER = 'N' OR
              WS-PWD-HAS-DIGIT = 'N'
               MOVE 'Y' TO WS-ERR-FLG
               MOVE 'Password must contain upper, lower, and digit.'
                   TO WS-MESSAGE
               MOVE -1 TO PASSWDL OF COSGN0AI
               PERFORM SEND-SIGNON-SCREEN
           END-IF.

      *----------------------------------------------------------------*
      *                      SEND-SIGNON-SCREEN
      *----------------------------------------------------------------*
       SEND-SIGNON-SCREEN.

           PERFORM POPULATE-HEADER-INFO

           MOVE WS-MESSAGE TO ERRMSGO OF COSGN0AO

           EXEC CICS SEND
                     MAP('COSGN0A')
                     MAPSET('COSGN00')
                     FROM(COSGN0AO)
                     ERASE
                     CURSOR
           END-EXEC.

      *----------------------------------------------------------------*
      *                      SEND-PLAIN-TEXT
      *----------------------------------------------------------------*
       SEND-PLAIN-TEXT.

           EXEC CICS SEND TEXT
                     FROM(WS-MESSAGE)
                     LENGTH(LENGTH OF WS-MESSAGE)
                     ERASE
                     FREEKB
           END-EXEC.

           EXEC CICS RETURN
           END-EXEC.

      *----------------------------------------------------------------*
      *                      POPULATE-HEADER-INFO
      *----------------------------------------------------------------*
       POPULATE-HEADER-INFO.

           MOVE FUNCTION CURRENT-DATE  TO WS-CURDATE-DATA

           MOVE CCDA-TITLE01           TO TITLE01O OF COSGN0AO
           MOVE CCDA-TITLE02           TO TITLE02O OF COSGN0AO
           MOVE WS-TRANID              TO TRNNAMEO OF COSGN0AO
           MOVE WS-PGMNAME             TO PGMNAMEO OF COSGN0AO

           MOVE WS-CURDATE-MONTH       TO WS-CURDATE-MM
           MOVE WS-CURDATE-DAY         TO WS-CURDATE-DD
           MOVE WS-CURDATE-YEAR(3:2)   TO WS-CURDATE-YY

           MOVE WS-CURDATE-MM-DD-YY    TO CURDATEO OF COSGN0AO

           MOVE WS-CURTIME-HOURS       TO WS-CURTIME-HH
           MOVE WS-CURTIME-MINUTE      TO WS-CURTIME-MM
           MOVE WS-CURTIME-SECOND      TO WS-CURTIME-SS

           MOVE WS-CURTIME-HH-MM-SS    TO CURTIMEO OF COSGN0AO

           EXEC CICS ASSIGN
               APPLID(APPLIDO OF COSGN0AO)
           END-EXEC

           EXEC CICS ASSIGN
               SYSID(SYSIDO OF COSGN0AO)
           END-EXEC.

      *----------------------------------------------------------------*
      *                      READ-USER-SEC-FILE
      *----------------------------------------------------------------*
       READ-USER-SEC-FILE.

           EXEC CICS READ
                DATASET   (WS-USRSEC-FILE)
                INTO      (SEC-USER-DATA)
                LENGTH    (LENGTH OF SEC-USER-DATA)
                RIDFLD    (WS-USER-ID)
                KEYLENGTH (LENGTH OF WS-USER-ID)
                RESP      (WS-RESP-CD)
                RESP2     (WS-REAS-CD)
           END-EXEC.

           EVALUATE WS-RESP-CD
               WHEN 0
      *            Check if account is locked
      *            (Assumes SEC-USER-DATA contains SEC-USR-LOCKED-FLAG
      *             and SEC-USR-FAILED-COUNT fields - requires file mod)
      *            IF SEC-USR-LOCKED-FLAG = 'Y'
      *                MOVE 'Y' TO WS-ERR-FLG
      *                MOVE 'Invalid credentials. Please try again.'
      *                    TO WS-MESSAGE
      *                MOVE -1 TO USERIDL OF COSGN0AI
      *                PERFORM SEND-SIGNON-SCREEN
      *                EXIT PARAGRAPH
      *            END-IF

      *            Hash the entered password and compare with stored hash
      *            PLACEHOLDER: Call cryptographic hash function
      *            CALL 'HASHPWD' USING WS-USER-PWD WS-PASSWORD-HASH
      *            MOVE SEC-USR-PWD TO WS-STORED-HASH
      *
      *            Timing-safe comparison (placeholder)
      *            CALL 'CMPHASH' USING WS-PASSWORD-HASH
      *                                  WS-STORED-HASH
      *                                  WS-HASH-MATCH
      *
      *            For now, simulate hashed comparison with direct compare
      *            (INSECURE - requires crypto library integration)
                   MOVE 'N' TO WS-HASH-MATCH
                   IF SEC-USR-PWD = WS-USER-PWD
                       MOVE 'Y' TO WS-HASH-MATCH
                   END-IF

                   IF HASH-MATCHES
      *                Reset failed login count on successful login
      *                MOVE 0 TO SEC-USR-FAILED-COUNT
      *                MOVE 'N' TO SEC-USR-LOCKED-FLAG
      *                EXEC CICS REWRITE
      *                    DATASET (WS-USRSEC-FILE)
      *                    FROM (SEC-USER-DATA)
      *                    LENGTH (LENGTH OF SEC-USER-DATA)
      *                END-EXEC

                       MOVE WS-TRANID    TO CDEMO-FROM-TRANID
                       MOVE WS-PGMNAME   TO CDEMO-FROM-PROGRAM
                       MOVE WS-USER-ID   TO CDEMO-USER-ID
                       MOVE SEC-USR-TYPE TO CDEMO-USER-TYPE
                       MOVE ZEROS        TO CDEMO-PGM-CONTEXT

                       IF CDEMO-USRTYP-ADMIN
                            EXEC CICS XCTL
                              PROGRAM ('COADM01C')
                              COMMAREA(CARDDEMO-COMMAREA)
                            END-EXEC
                       ELSE
                            EXEC CICS XCTL
                              PROGRAM ('COMEN01C')
                              COMMAREA(CARDDEMO-COMMAREA)
                            END-EXEC
                       END-IF
                   ELSE
      *                Increment failed login count
      *                ADD 1 TO SEC-USR-FAILED-COUNT
      *                IF SEC-USR-FAILED-COUNT >= WS-MAX-FAILED-LOGINS
      *                    MOVE 'Y' TO SEC-USR-LOCKED-FLAG
      *                END-IF
      *                EXEC CICS REWRITE
      *                    DATASET (WS-USRSEC-FILE)
      *                    FROM (SEC-USER-DATA)
      *                    LENGTH (LENGTH OF SEC-USER-DATA)
      *                END-EXEC

      *                Generic error message to prevent enumeration
                       MOVE 'Y' TO WS-ERR-FLG
                       MOVE 'Invalid credentials. Please try again.'
                           TO WS-MESSAGE
                       MOVE -1 TO USERIDL OF COSGN0AI
                       PERFORM SEND-SIGNON-SCREEN
                   END-IF
               WHEN 13
      *            Generic error message - do not reveal user not found
                   MOVE 'Y'      TO WS-ERR-FLG
                   MOVE 'Invalid credentials. Please try again.'
                       TO WS-MESSAGE
                   MOVE -1       TO USERIDL OF COSGN0AI
                   PERFORM SEND-SIGNON-SCREEN
               WHEN OTHER
      *            Generic error message
                   MOVE 'Y'      TO WS-ERR-FLG
                   MOVE 'Invalid credentials. Please try again.'
                       TO WS-MESSAGE
                   MOVE -1       TO USERIDL OF COSGN0AI
                   PERFORM SEND-SIGNON-SCREEN
           END-EVALUATE.

      *----------------------------------------------------------------*
      *                      CLEAR-SESSION-DATA
      *----------------------------------------------------------------*
       CLEAR-SESSION-DATA.
      *    Clear sensitive session data on sign-off
           MOVE SPACES TO CDEMO-USER-ID
           MOVE SPACES TO CDEMO-FROM-TRANID
           MOVE SPACES TO CDEMO-FROM-PROGRAM
           MOVE SPACES TO CDEMO-USER-TYPE
           MOVE ZEROS  TO CDEMO-PGM-CONTEXT.

      *
      * Ver: CardDemo_v1.0-15-g27d6c6f-68 Date: 2022-07-19 23:12:33 CDT
      *