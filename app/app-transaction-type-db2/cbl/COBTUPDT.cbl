**************************************** *************************00010032
      * Program:     COBTUPDT.CBL                                      *00020032
      * Layer:       Business logic                                    *00030032
      * Function:    Update Transaction type based on user input       *00040032
      ******************************************************************00050032
      * Copyright Amazon.com, Inc. or its affiliates.                   00060032
      * All Rights Reserved.                                            00070032
      *                                                                 00080032
      * Licensed under the Apache License, Version 2.0 (the "License"). 00090032
      * You may not use this file except in compliance with the License.00100032
      * You may obtain a copy of the License at                         00110032
      *                                                                 00120032
      *    http://www.apache.org/licenses/LICENSE-2.0                   00130032
      *                                                                 00140032
      * Unless required by applicable law or agreed to in writing,      00150032
      * software distributed under the License is distributed on an     00160032
      * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND,    00170032
      * either express or implied. See the License for the specific     00180032
      * language governing permissions and limitations under the License00190032
      ******************************************************************00200032
                                                                        00210032
       IDENTIFICATION DIVISION.                                         00220032
       PROGRAM-ID. COBTUPDT.                                            00230032
                                                                        00240032
       ENVIRONMENT DIVISION.                                            00250032
                                                                        00260032
       CONFIGURATION SECTION.                                           00270032
                                                                        00280032
       INPUT-OUTPUT SECTION.                                            00290032
       FILE-CONTROL.                                                    00300032
           SELECT TR-RECORD ASSIGN TO INPFILE                           00310032
                  ORGANIZATION IS SEQUENTIAL                            00311032
                  ACCESS MODE IS SEQUENTIAL                             00312032
                  FILE STATUS IS WS-INF-STATUS.                         00313032
                                                                        00314032
       DATA DIVISION.                                                   00315032
                                                                        00316032
       FILE SECTION.                                                    00317032
       FD TR-RECORD RECORDING MODE F.                                   00318032
       01 WS-INPUT-VARS.                                                00319032
          05 INPUT-TYPE                            PIC X(1)             00320032
                                                   VALUE SPACES.        00330032
          05 INPUT-TR-NUMBER                       PIC X(2)             00340032
                                                   VALUE SPACES.        00350032
          05 INPUT-TR-DESC                         PIC X(50)            00360032
                                                   VALUE SPACES.        00370032
                                                                        00380032
       WORKING-STORAGE SECTION.                                         00390032
                                                                        00400032
            EXEC SQL                                                    00410032
                INCLUDE SQLCA                                           00420032
            END-EXEC                                                    00430032
                                                                        00440032
            EXEC SQL INCLUDE DCLTRTYP END-EXEC                          00450046
                                                                        00451046
                                                                        00460032
       01 FLAGS.                                                        00470032
         05 LASTREC                                PIC X(1)             00480032
                                                   VALUE SPACES.        00490032
         05 VALIDATION-ERROR                       PIC X(1)             00491032
                                                   VALUE SPACES.        00492032
       01 WORKING-VARIABLES.                                            00500032
         05 WS-RETURN-MSG                          PIC X(80)            00510044
                                                   VALUE SPACES.        00520032
         05 WS-RECORD-COUNT                        PIC 9(9) COMP        00521032
                                                   VALUE ZERO.          00522032
         05 WS-COMMIT-THRESHOLD                    PIC 9(9) COMP        00523032
                                                   VALUE 100.           00524032
                                                                        00530032
       01 WS-MISC-VARS.                                                 00540032
         05 WS-VAR-SQLCODE                     PIC ----9.               00550032
         05 WS-I                               PIC 9(2) COMP.           00551032
                                                                        00560032
       01  WS-INF-STATUS.                                               00570032
           05  WS-INF-STAT1       PIC X.                                00580032
           05  WS-INF-STAT2       PIC X.                                00590032
                                                                        00590133
       01 WS-INPUT-REC.                                                 00591033
          05 INPUT-REC-TYPE                        PIC X(1)             00592033
                                                   VALUE SPACES.        00593033
          05 INPUT-REC-NUMBER                      PIC X(2)             00594033
                                                   VALUE SPACES.        00595033
          05 INPUT-REC-DESC                        PIC X(50)            00596033
                                                   VALUE SPACES.        00597033
                                                                        00598033
                                                                        00600032
       PROCEDURE DIVISION.                                              00610032
                                                                        00620032
       0001-OPEN-FILES.                                                 00630032
           OPEN INPUT TR-RECORD.                                        00640032
           IF WS-INF-STATUS = '00' THEN                                 00650032
              DISPLAY 'OPEN FILE OK'                                    00660046
           ELSE                                                         00670032
              DISPLAY 'OPEN FILE NOT OK'                                00680046
           END-IF                                                       00690032
           EXIT.                                                        00700032
                                                                        00710032
       1001-READ-NEXT-RECORDS.                                          00720032
               PERFORM 1002-READ-RECORDS                                00740443
            PERFORM UNTIL LASTREC = 'Y'                                 00740543
               PERFORM 1003-TREAT-RECORD                                00740743
               PERFORM 1002-READ-RECORDS                                00740843
            END-PERFORM.                                                00740943
            PERFORM 2002-FINAL-COMMIT                                   00741041
            PERFORM 2001-CLOSE-STOP                                     00742041
            EXIT.                                                       00780032
            STOP RUN.                                                   00790041
       1002-READ-RECORDS.                                               00840032
           READ TR-RECORD NEXT RECORD INTO WS-INPUT-REC                 00850033
           AT END MOVE 'Y' TO LASTREC                                   00860032
           END-READ.                                                    00870032
           IF LASTREC NOT EQUAL TO 'Y' THEN                             00870144
              DISPLAY 'PROCESSING   ' WS-INPUT-REC                      00871044
           END-IF.                                                      00872044
           EXIT.                                                        00880032
                                                                        00890032
       1003-TREAT-RECORD.                                               00900032
           MOVE 'N' TO VALIDATION-ERROR.                                00901032
           PERFORM 1004-VALIDATE-INPUT.                                 00902032
           IF VALIDATION-ERROR = 'Y' THEN                               00903032
              PERFORM 9998-ROLLBACK-ABEND                               00904032
           END-IF.                                                      00905032
           EVALUATE INPUT-REC-TYPE                                      00910033
               WHEN 'A'                                                 00920034
                   DISPLAY 'ADDING RECORD'                              00921034
                   PERFORM 10031-INSERT-DB                              00930032
               WHEN 'U'                                                 00940032
                   DISPLAY 'UPDATING RECORD'                            00941034
                   PERFORM 10032-UPDATE-DB                              00950032
               WHEN 'D'                                                 00960032
                   DISPLAY 'DELETING RECORD'                            00961034
                   PERFORM 10033-DELETE-DB                              00970032
               WHEN '*'                                                 00971045
                   DISPLAY 'IGNORING COMMENTED LINE'                    00972045
               WHEN OTHER                                               00980032
                  STRING                                                00990032
                  'ERROR: TYPE NOT VALID'                               01000041
                  DELIMITED BY SIZE                                     01020032
                  INTO WS-RETURN-MSG                                    01030032
                  END-STRING                                            01040032
                  PERFORM 9998-ROLLBACK-ABEND                           01050032
           END-EVALUATE.                                                01060032
           ADD 1 TO WS-RECORD-COUNT.                                    01061032
           IF WS-RECORD-COUNT >= WS-COMMIT-THRESHOLD THEN               01062032
              PERFORM 2003-COMMIT-WORK                                  01063032
              MOVE ZERO TO WS-RECORD-COUNT                              01064032
           END-IF.                                                      01065032
           EXIT.                                                        01070032
                                                                        01071032
       1004-VALIDATE-INPUT.                                             01072032
      ******************************************************************01073032
      * VALIDATE INPUT RECORD FORMAT AND CONTENT                        01074032
      ******************************************************************01075032
           IF INPUT-REC-TYPE NOT = 'A' AND                              01076032
              INPUT-REC-TYPE NOT = 'U' AND                              01077032
              INPUT-REC-TYPE NOT = 'D' AND                              01078032
              INPUT-REC-TYPE NOT = '*' THEN                             01079032
              STRING 'INVALID RECORD TYPE: ' INPUT-REC-TYPE             01080032
                 DELIMITED BY SIZE INTO WS-RETURN-MSG                   01081032
              END-STRING                                                01082032
              MOVE 'Y' TO VALIDATION-ERROR                              01083032
              EXIT                                                      01084032
           END-IF.                                                      01085032
           IF INPUT-REC-TYPE = '*' THEN                                 01086032
              EXIT                                                      01087032
           END-IF.                                                      01088032
           IF INPUT-REC-NUMBER = SPACES THEN                            01089032
              STRING 'TRANSACTION NUMBER IS REQUIRED'                   01089132
                 DELIMITED BY SIZE INTO WS-RETURN-MSG                   01089232
              END-STRING                                                01089332
              MOVE 'Y' TO VALIDATION-ERROR                              01089432
              EXIT                                                      01089532
           END-IF.                                                      01089632
           PERFORM VARYING WS-I FROM 1 BY 1 UNTIL WS-I > 2              01089732
              IF INPUT-REC-NUMBER(WS-I:1) NOT NUMERIC AND               01089832
                 INPUT-REC-NUMBER(WS-I:1) NOT ALPHABETIC THEN           01089932
                 STRING 'INVALID CHAR IN TR NUMBER'                     01089A32
                    DELIMITED BY SIZE INTO WS-RETURN-MSG                01089B32
                 END-STRING                                             01089C32
                 MOVE 'Y' TO VALIDATION-ERROR                           01089D32
                 EXIT                                                   01089E32
              END-IF                                                    01089F32
           END-PERFORM.                                                 01089G32
           IF INPUT-REC-TYPE = 'A' OR INPUT-REC-TYPE = 'U' THEN         01089H32
              IF INPUT-REC-DESC = SPACES THEN                           01089I32
                 STRING 'DESCRIPTION IS REQUIRED FOR ADD/UPDATE'        01089J32
                    DELIMITED BY SIZE INTO WS-RETURN-MSG                01089K32
                 END-STRING                                             01089L32
                 MOVE 'Y' TO VALIDATION-ERROR                           01089M32
                 EXIT                                                   01089N32
              END-IF                                                    01089O32
           END-IF.                                                      01089P32
           EXIT.                                                        01089Q32
                                                                        01080041
       10031-INSERT-DB.                                                 01090032
      ******************************************************************01100032
      * SQL TO INSERT THE RECORD                                        01110032
      ******************************************************************01120032
      *                                                                 01130032
           EXEC SQL                                                     01140032
                INSERT INTO CARDDEMO.TRANSACTION_TYPE                   01150032
                (                                                       01160032
                TR_TYPE,                                                01170032
                TR_DESCRIPTION                                          01180032
                )                                                       01190032
                VALUES                                                  01200032
                (                                                       01210032
                :INPUT-REC-NUMBER,                                      01220033
                :INPUT-REC-DESC                                         01230033
                )                                                       01240032
           END-EXEC.                                                    01250034
           MOVE SQLCODE TO WS-VAR-SQLCODE                               01260032
                                                                        01270032
           EVALUATE TRUE                                                01310032
               WHEN SQLCODE = ZERO                                      01320032
                  DISPLAY 'RECORD INSERTED SUCCESSFULLY'                01330044
               WHEN SQLCODE < 0                                         01340032
                  STRING                                                01350032
                  'Error accessing:'                                    01360032
                  ' TRANSACTION_TYPE table. SQLCODE:'                   01370032
                  WS-VAR-SQLCODE                                        01380044
                  DELIMITED BY SIZE                                     01410032
                  INTO WS-RETURN-MSG                                    01420032
                  END-STRING                                            01430032
                  PERFORM 9998-ROLLBACK-ABEND                           01440032
           END-EVALUATE                                                 01450032
           EXIT.                                                        01460032
                                                                        01470032
       10032-UPDATE-DB.                                                 01480032
      ******************************************************************01490032
      * SQL TO UPDATE THE RECORD                                        01500032
      ******************************************************************01510032
      *                                                                 01520032
           EXEC SQL                                                     01522037
                UPDATE CARDDEMO.TRANSACTION_TYPE                        01523040
                   SET TR_DESCRIPTION = :INPUT-REC-DESC                 01524041
                 WHERE TR_TYPE = :INPUT-REC-NUMBER                      01525041
           END-EXEC                                                     01526037
           MOVE SQLCODE TO WS-VAR-SQLCODE                               01580032
           EVALUATE TRUE                                                01630032
               WHEN SQLCODE = ZERO                                      01640032
                  DISPLAY 'RECORD UPDATED SUCCESSFULLY'                 01650044
               WHEN SQLCODE = +100                                      01660032
                  STRING 'No records found.' DELIMITED BY SIZE          01670041
                     INTO WS-RETURN-MSG                                 01680041
                  END-STRING                                            01690041
                  PERFORM 9998-ROLLBACK-ABEND                           01700041
               WHEN SQLCODE < 0                                         01710032
                  STRING                                                01711044
                  'Error accessing:'                                    01712044
                  ' TRANSACTION_TYPE table. SQLCODE:'                   01713044
                  WS-VAR-SQLCODE                                        01714044
                  DELIMITED BY SIZE                                     01717044
                  INTO WS-RETURN-MSG                                    01718044
                  END-STRING                                            01719044
                  PERFORM 9998-ROLLBACK-ABEND                           01719144
           END-EVALUATE                                                 01820032
           EXIT.                                                        01830032
       10033-DELETE-DB.                                                 01850032
      ******************************************************************01860032
      * SQL TO DELETE THE RECORD                                        01870032
      ******************************************************************01880032
      *                                                                 01890032
           EXEC SQL                                                     01900032
                DELETE FROM CARDDEMO.TRANSACTION_TYPE                   01910032
                 WHERE TR_TYPE = :INPUT-REC-NUMBER                      01920033
           END-EXEC.                                                    01930034
           MOVE SQLCODE TO WS-VAR-SQLCODE                               01940032
                                                                        01950032
           EVALUATE TRUE                                                01990032
               WHEN SQLCODE = ZERO                                      02000032
                  DISPLAY 'RECORD DELETED SUCCESSFULLY'                 02010032
               WHEN SQLCODE = +100                                      02020032
               STRING 'No records found.' DELIMITED BY SIZE             02030032
               INTO WS-RETURN-MSG                                       02040032
               END-STRING                                               02050032
               PERFORM 9998-ROLLBACK-ABEND                              02060032
                                                                        02070032
               WHEN SQLCODE < 0                                         02080032
                  STRING                                                02090032
                  'Error accessing:'                                    02100032
                  ' TRANSACTION_TYPE table. SQLCODE:'                   02110032
                  WS-VAR-SQLCODE                                        02120032
                  DELIMITED BY SIZE                                     02150032
                  INTO WS-RETURN-MSG                                    02160032
                  END-STRING                                            02170032
                  PERFORM 9998-ROLLBACK-ABEND                           02180032
           END-EVALUATE                                                 02190032
           EXIT.                                                        02200032
                                                                        02210032
       2001-CLOSE-STOP.                                                 02261041
           CLOSE TR-RECORD.                                             02262041
           EXIT.                                                        02264041
                                                                        02265041
       2002-FINAL-COMMIT.                                               02266041
           EXEC SQL COMMIT END-EXEC.                                    02267041
           IF SQLCODE NOT = ZERO THEN                                   02268041
              DISPLAY 'FINAL COMMIT FAILED. SQLCODE: ' SQLCODE          02269041
              MOVE 12 TO RETURN-CODE                                    02269141
           ELSE                                                         02269241
              DISPLAY 'FINAL COMMIT SUCCESSFUL'                         02269341
           END-IF.                                                      02269441
           EXIT.                                                        02269541
                                                                        02269641
       2003-COMMIT-WORK.                                                02269741
           EXEC SQL COMMIT END-EXEC.                                    02269841
           IF SQLCODE NOT = ZERO THEN                                   02269941
              DISPLAY 'COMMIT FAILED. SQLCODE: ' SQLCODE                02269A41
              STRING 'COMMIT FAILED AT CHECKPOINT'                      02269B41
                 DELIMITED BY SIZE INTO WS-RETURN-MSG                   02269C41
              END-STRING                                                02269D41
              PERFORM 9998-ROLLBACK-ABEND                               02269E41
           ELSE                                                         02269F41
              DISPLAY 'CHECKPOINT COMMIT SUCCESSFUL'                    02269G41
           END-IF.                                                      02269H41
           EXIT.                                                        02269I41
                                                                        02230032
       9998-ROLLBACK-ABEND.                                             02231032
           EXEC SQL ROLLBACK END-EXEC.                                  02232032
           DISPLAY 'ROLLBACK EXECUTED'.                                 02233032
           DISPLAY WS-RETURN-MSG.                                       02234032
           MOVE 12 TO RETURN-CODE.                                      02235032
           PERFORM 2001-CLOSE-STOP.                                     02236032
           STOP RUN.                                                    02237032
                                                                        02238032
       9999-ABEND.                                                      02240032
           DISPLAY WS-RETURN-MSG.                                       02250032
           MOVE 12 TO RETURN-CODE                                       02251044
           PERFORM 2001-CLOSE-STOP.                                     02252044
           STOP RUN.                                                    02260032
                                                                        02270032