******************************************************************
      * PROGRAM     : COBSWAIT.CBL
      * Application : CardDemo
      * Type        : BATCH COBOL Program
      * FUNCTION    : UTILITY PROGRAM TO WAIT (PARM IN CENTISECONDS)
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
       IDENTIFICATION DIVISION.                                         00010000
       PROGRAM-ID. COBSWAIT.                                             0002000
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.                                            00030000
       DATA DIVISION.                                                   00030900

       WORKING-STORAGE SECTION.
      *-----------------------------------------------------------------
       01 MVSWAIT-TIME                    PIC 9(8) COMP.
       01 PARM-VALUE                      PIC X(8).
       01 WS-NUMERIC-CHECK                PIC X(8).
       01 WS-NUMERIC-VALUE                PIC 9(8).
       01 WS-MAX-WAIT                     PIC 9(8) COMP VALUE 6000.
       01 WS-IDX                          PIC 9(2).
       01 WS-VALID-FLAG                   PIC X VALUE 'Y'.


       PROCEDURE DIVISION.                                              00040000

           ACCEPT PARM-VALUE      FROM SYSIN.
           
      *    VALIDATE INPUT IS NUMERIC
           MOVE 'Y' TO WS-VALID-FLAG.
           MOVE PARM-VALUE TO WS-NUMERIC-CHECK.
           INSPECT WS-NUMERIC-CHECK REPLACING LEADING SPACES BY ZEROS.
           
           PERFORM VARYING WS-IDX FROM 1 BY 1 UNTIL WS-IDX > 8
               IF WS-NUMERIC-CHECK(WS-IDX:1) NOT NUMERIC
                   MOVE 'N' TO WS-VALID-FLAG
               END-IF
           END-PERFORM.
           
           IF WS-VALID-FLAG = 'N'
               DISPLAY 'ERROR: INVALID NUMERIC INPUT'
               MOVE 12 TO RETURN-CODE
               STOP RUN
           END-IF.
           
      *    CONVERT AND VALIDATE RANGE (MAX 60 SECONDS = 6000 CENTISEC)
           MOVE FUNCTION NUMVAL(WS-NUMERIC-CHECK) TO WS-NUMERIC-VALUE.
           
           IF WS-NUMERIC-VALUE > WS-MAX-WAIT
               DISPLAY 'ERROR: WAIT TIME EXCEEDS MAXIMUM (6000)'
               MOVE 12 TO RETURN-CODE
               STOP RUN
           END-IF.
           
           MOVE WS-NUMERIC-VALUE TO MVSWAIT-TIME.
           CALL 'MVSWAIT'       USING MVSWAIT-TIME.

           STOP RUN.                                                    00060000