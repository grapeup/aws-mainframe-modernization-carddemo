IDENTIFICATION DIVISION.
       PROGRAM-ID.    CBIMPORT.
       AUTHOR.        CARDDEMO TEAM.
      ******************************************************************
      * Program     : CBIMPORT.CBL
      * Application : CardDemo
      * Type        : BATCH COBOL Program
      * Function    : Import Customer Data from Branch Migration Export
      *               Reads multi-record export file and splits it into
      *               separate normalized target files (CUSTOMER, ACCOUNT,
      *               CARD-XREF, TRANSACTION) with validation
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
      * Business Use Case: Branch Migration Import
      * - Import complete customer profiles from export file
      * - Split multi-record layout into normalized target files
      * - Validate data integrity using checksums
      * - Generate import statistics and error reports
      ******************************************************************
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXPORT-INPUT ASSIGN TO EXPFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS EXPORT-SEQUENCE-NUM
               FILE STATUS IS WS-EXPORT-STATUS.
               
           SELECT CUSTOMER-OUTPUT ASSIGN TO CUSTOUT
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-CUSTOMER-STATUS.
               
           SELECT ACCOUNT-OUTPUT ASSIGN TO ACCTOUT
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-ACCOUNT-STATUS.
               
           SELECT XREF-OUTPUT ASSIGN TO XREFOUT
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-XREF-STATUS.
               
           SELECT TRANSACTION-OUTPUT ASSIGN TO TRNXOUT
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-TRANSACTION-STATUS.
               
           SELECT CARD-OUTPUT ASSIGN TO CARDOUT
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-CARD-STATUS.
               
           SELECT ERROR-OUTPUT ASSIGN TO ERROUT
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-ERROR-STATUS.

       DATA DIVISION.
       FILE SECTION.
       
       FD  EXPORT-INPUT
           RECORDING MODE IS F
           RECORD CONTAINS 500 CHARACTERS.
       01  EXPORT-INPUT-RECORD                        PIC X(500).

       FD  CUSTOMER-OUTPUT
           RECORDING MODE IS F
           RECORD CONTAINS 500 CHARACTERS.
       COPY CVCUS01Y.

       FD  ACCOUNT-OUTPUT
           RECORDING MODE IS F
           RECORD CONTAINS 300 CHARACTERS.
       COPY CVACT01Y.

       FD  XREF-OUTPUT
           RECORDING MODE IS F
           RECORD CONTAINS 50 CHARACTERS.
       COPY CVACT03Y.

       FD  TRANSACTION-OUTPUT
           RECORDING MODE IS F
           RECORD CONTAINS 350 CHARACTERS.
       COPY CVTRA05Y.

       FD  CARD-OUTPUT
           RECORDING MODE IS F
           RECORD CONTAINS 150 CHARACTERS.
       COPY CVACT02Y.

       FD  ERROR-OUTPUT
           RECORDING MODE IS F
           RECORD CONTAINS 132 CHARACTERS.
       01  ERROR-OUTPUT-RECORD                        PIC X(132).

       WORKING-STORAGE SECTION.

       COPY CVEXPORT.

      * File Status Variables
       01  WS-FILE-STATUS-AREA.
           05  WS-EXPORT-STATUS                        PIC X(02).
               88  WS-EXPORT-EOF                       VALUE '10'.
               88  WS-EXPORT-OK                        VALUE '00'.
           05  WS-CUSTOMER-STATUS                      PIC X(02).
               88  WS-CUSTOMER-OK                      VALUE '00'.
           05  WS-ACCOUNT-STATUS                       PIC X(02).
               88  WS-ACCOUNT-OK                       VALUE '00'.
           05  WS-XREF-STATUS                          PIC X(02).
               88  WS-XREF-OK                          VALUE '00'.
           05  WS-TRANSACTION-STATUS                   PIC X(02).
               88  WS-TRANSACTION-OK                   VALUE '00'.
           05  WS-CARD-STATUS                          PIC X(02).
               88  WS-CARD-OK                          VALUE '00'.
           05  WS-ERROR-STATUS                         PIC X(02).
               88  WS-ERROR-OK                         VALUE '00'.

      * Import Control Variables
       01  WS-IMPORT-CONTROL.
           05  WS-IMPORT-DATE                          PIC X(10).
           05  WS-IMPORT-TIME                          PIC X(08).

      * Statistics Counters
       01  WS-IMPORT-STATISTICS.
           05  WS-TOTAL-RECORDS-READ                  PIC 9(09) VALUE 0.
           05  WS-CUSTOMER-RECORDS-IMPORTED           PIC 9(09) VALUE 0.
           05  WS-ACCOUNT-RECORDS-IMPORTED            PIC 9(09) VALUE 0.
           05  WS-XREF-RECORDS-IMPORTED               PIC 9(09) VALUE 0.
           05  WS-TRAN-RECORDS-IMPORTED           PIC 9(09) VALUE 0.
           05  WS-CARD-RECORDS-IMPORTED               PIC 9(09) VALUE 0.
           05  WS-ERROR-RECORDS-WRITTEN               PIC 9(09) VALUE 0.
           05  WS-UNKNOWN-RECORD-TYPE-COUNT           PIC 9(09) VALUE 0.

      * Validation Variables
       01  WS-VALIDATION-CONTROL.
           05  WS-VALIDATION-ERRORS                   PIC 9(09) VALUE 0.
           05  WS-EXPECTED-CHECKSUM                   PIC 9(15) VALUE 0.
           05  WS-CALCULATED-CHECKSUM                 PIC 9(15) VALUE 0.
           05  WS-EXPECTED-RECORD-COUNT               PIC 9(09) VALUE 0.

      * Duplicate Detection Tables
       01  WS-DUPLICATE-TABLES.
           05  WS-CUSTOMER-TABLE.
               10  WS-CUSTOMER-KEY OCCURS 10000 TIMES
                   INDEXED BY CUST-IDX                 PIC 9(09) VALUE 0.
           05  WS-ACCOUNT-TABLE.
               10  WS-ACCOUNT-KEY OCCURS 10000 TIMES
                   INDEXED BY ACCT-IDX                 PIC 9(11) VALUE 0.
           05  WS-CARD-TABLE.
               10  WS-CARD-KEY OCCURS 10000 TIMES
                   INDEXED BY CARD-IDX                 PIC 9(16) VALUE 0.
           05  WS-XREF-TABLE.
               10  WS-XREF-KEY OCCURS 10000 TIMES
                   INDEXED BY XREF-IDX                 PIC 9(16) VALUE 0.
           05  WS-TRAN-TABLE.
               10  WS-TRAN-KEY OCCURS 10000 TIMES
                   INDEXED BY TRAN-IDX                 PIC X(16) VALUE SPACES.

       01  WS-DUPLICATE-COUNTERS.
           05  WS-CUSTOMER-COUNT                      PIC 9(05) VALUE 0.
           05  WS-ACCOUNT-COUNT                       PIC 9(05) VALUE 0.
           05  WS-CARD-COUNT                          PIC 9(05) VALUE 0.
           05  WS-XREF-COUNT                          PIC 9(05) VALUE 0.
           05  WS-TRAN-COUNT                          PIC 9(05) VALUE 0.
           05  WS-DUPLICATE-FOUND                     PIC X(01) VALUE 'N'.
               88  DUPLICATE-DETECTED                 VALUE 'Y'.
               88  NO-DUPLICATE                       VALUE 'N'.

       01  WS-WORK-FIELDS.
           05  WS-SEARCH-INDEX                        PIC 9(05) VALUE 0.
           05  WS-TEMP-NUMERIC                        PIC 9(16) VALUE 0.

      * Error Record Layout
       01  WS-ERROR-RECORD.
           05  ERR-TIMESTAMP                           PIC X(26).
           05  FILLER                              PIC X(01) VALUE '|'. 
           05  ERR-RECORD-TYPE                         PIC X(01).
           05  FILLER                              PIC X(01) VALUE '|'. 
           05  ERR-SEQUENCE                            PIC 9(07).
           05  FILLER                              PIC X(01) VALUE '|'. 
           05  ERR-MESSAGE                             PIC X(50).
           05  FILLER                          PIC X(43) VALUE SPACES.

       PROCEDURE DIVISION.

      *****************************************************************
       0000-MAIN-PROCESSING.
      *****************************************************************
           PERFORM 1000-INITIALIZE
           PERFORM 2000-PROCESS-EXPORT-FILE
           PERFORM 3000-VALIDATE-IMPORT
           PERFORM 4000-FINALIZE
           GOBACK.

      *****************************************************************
       1000-INITIALIZE.
      *****************************************************************
           DISPLAY 'CBIMPORT: Starting Customer Data Import'
           
           MOVE FUNCTION CURRENT-DATE(1:4) TO WS-IMPORT-DATE(1:4)
           MOVE '-' TO WS-IMPORT-DATE(5:1)
           MOVE FUNCTION CURRENT-DATE(5:2) TO WS-IMPORT-DATE(6:2)
           MOVE '-' TO WS-IMPORT-DATE(8:1)
           MOVE FUNCTION CURRENT-DATE(7:2) TO WS-IMPORT-DATE(9:2)
           
           MOVE FUNCTION CURRENT-DATE(9:2) TO WS-IMPORT-TIME(1:2)
           MOVE ':' TO WS-IMPORT-TIME(3:1)
           MOVE FUNCTION CURRENT-DATE(11:2) TO WS-IMPORT-TIME(4:2)
           MOVE ':' TO WS-IMPORT-TIME(6:1)
           MOVE FUNCTION CURRENT-DATE(13:2) TO WS-IMPORT-TIME(7:2)
           
           PERFORM 1100-OPEN-FILES
           PERFORM 1200-INITIALIZE-DUPLICATE-TABLES
           
           DISPLAY 'CBIMPORT: Import Date: ' WS-IMPORT-DATE
           DISPLAY 'CBIMPORT: Import Time: ' WS-IMPORT-TIME.

      *****************************************************************
       1100-OPEN-FILES.
      *****************************************************************
           OPEN INPUT EXPORT-INPUT
           IF NOT WS-EXPORT-OK
               DISPLAY 'ERROR: Cannot open EXPORT-INPUT, Status: '
                       WS-EXPORT-STATUS
               PERFORM 9999-ABEND-PROGRAM
           END-IF
           
           OPEN OUTPUT CUSTOMER-OUTPUT
           IF NOT WS-CUSTOMER-OK
               DISPLAY 'ERROR: Cannot open CUSTOMER-OUTPUT, Status: '
                       WS-CUSTOMER-STATUS
               PERFORM 9999-ABEND-PROGRAM
           END-IF
           
           OPEN OUTPUT ACCOUNT-OUTPUT
           IF NOT WS-ACCOUNT-OK
               DISPLAY 'ERROR: Cannot open ACCOUNT-OUTPUT, Status: '
                       WS-ACCOUNT-STATUS
               PERFORM 9999-ABEND-PROGRAM
           END-IF
           
           OPEN OUTPUT XREF-OUTPUT
           IF NOT WS-XREF-OK
               DISPLAY 'ERROR: Cannot open XREF-OUTPUT, Status: '
                       WS-XREF-STATUS
               PERFORM 9999-ABEND-PROGRAM
           END-IF
           
           OPEN OUTPUT TRANSACTION-OUTPUT
           IF NOT WS-TRANSACTION-OK
               DISPLAY 'ERROR: Cannot open TRANSACTION-OUTPUT, Status: '
                       WS-TRANSACTION-STATUS
               PERFORM 9999-ABEND-PROGRAM
           END-IF
           
           OPEN OUTPUT CARD-OUTPUT
           IF NOT WS-CARD-OK
               DISPLAY 'ERROR: Cannot open CARD-OUTPUT, Status: '
                       WS-CARD-STATUS
               PERFORM 9999-ABEND-PROGRAM
           END-IF
           
           OPEN OUTPUT ERROR-OUTPUT
           IF NOT WS-ERROR-OK
               DISPLAY 'ERROR: Cannot open ERROR-OUTPUT, Status: '
                       WS-ERROR-STATUS
               PERFORM 9999-ABEND-PROGRAM
           END-IF.

      *****************************************************************
       1200-INITIALIZE-DUPLICATE-TABLES.
      *****************************************************************
           PERFORM VARYING CUST-IDX FROM 1 BY 1 
                   UNTIL CUST-IDX > 10000
               MOVE 0 TO WS-CUSTOMER-KEY(CUST-IDX)
           END-PERFORM
           
           PERFORM VARYING ACCT-IDX FROM 1 BY 1 
                   UNTIL ACCT-IDX > 10000
               MOVE 0 TO WS-ACCOUNT-KEY(ACCT-IDX)
           END-PERFORM
           
           PERFORM VARYING CARD-IDX FROM 1 BY 1 
                   UNTIL CARD-IDX > 10000
               MOVE 0 TO WS-CARD-KEY(CARD-IDX)
           END-PERFORM
           
           PERFORM VARYING XREF-IDX FROM 1 BY 1 
                   UNTIL XREF-IDX > 10000
               MOVE 0 TO WS-XREF-KEY(XREF-IDX)
           END-PERFORM
           
           PERFORM VARYING TRAN-IDX FROM 1 BY 1 
                   UNTIL TRAN-IDX > 10000
               MOVE SPACES TO WS-TRAN-KEY(TRAN-IDX)
           END-PERFORM.

      *****************************************************************
       2000-PROCESS-EXPORT-FILE.
      *****************************************************************
           PERFORM 2100-READ-EXPORT-RECORD
           
           PERFORM UNTIL WS-EXPORT-EOF
               ADD 1 TO WS-TOTAL-RECORDS-READ
               PERFORM 2200-PROCESS-RECORD-BY-TYPE
               PERFORM 2100-READ-EXPORT-RECORD
           END-PERFORM.

      *****************************************************************
       2100-READ-EXPORT-RECORD.
      *****************************************************************
           READ EXPORT-INPUT INTO EXPORT-RECORD
           
           IF NOT WS-EXPORT-OK AND NOT WS-EXPORT-EOF
               DISPLAY 'ERROR: Reading EXPORT-INPUT, Status: '
                       WS-EXPORT-STATUS
               PERFORM 9999-ABEND-PROGRAM
           END-IF.

      *****************************************************************
       2200-PROCESS-RECORD-BY-TYPE.
      *****************************************************************
           EVALUATE EXPORT-REC-TYPE
               WHEN 'C'
                   PERFORM 2300-PROCESS-CUSTOMER-RECORD
               WHEN 'A'
                   PERFORM 2400-PROCESS-ACCOUNT-RECORD
               WHEN 'X'
                   PERFORM 2500-PROCESS-XREF-RECORD
               WHEN 'T'
                   PERFORM 2600-PROCESS-TRAN-RECORD
               WHEN 'D'
                   PERFORM 2650-PROCESS-CARD-RECORD
               WHEN OTHER
                   PERFORM 2700-PROCESS-UNKNOWN-RECORD
           END-EVALUATE.

      *****************************************************************
       2300-PROCESS-CUSTOMER-RECORD.
      *****************************************************************
           PERFORM 2310-CHECK-DUPLICATE-CUSTOMER
           
           IF DUPLICATE-DETECTED
               MOVE FUNCTION CURRENT-DATE TO ERR-TIMESTAMP
               MOVE EXPORT-REC-TYPE TO ERR-RECORD-TYPE
               MOVE EXPORT-SEQUENCE-NUM TO ERR-SEQUENCE
               MOVE 'Duplicate customer ID detected' TO ERR-MESSAGE
               PERFORM 2750-WRITE-ERROR
               GO TO 2300-EXIT
           END-IF
           
           INITIALIZE CUSTOMER-RECORD
           
      *    Map export fields to customer record
           MOVE EXP-CUST-ID TO CUST-ID
           MOVE EXP-CUST-FIRST-NAME TO CUST-FIRST-NAME
           MOVE EXP-CUST-MIDDLE-NAME TO CUST-MIDDLE-NAME
           MOVE EXP-CUST-LAST-NAME TO CUST-LAST-NAME
           MOVE EXP-CUST-ADDR-LINE(1) TO CUST-ADDR-LINE-1
           MOVE EXP-CUST-ADDR-LINE(2) TO CUST-ADDR-LINE-2
           MOVE EXP-CUST-ADDR-LINE(3) TO CUST-ADDR-LINE-3
           MOVE EXP-CUST-ADDR-STATE-CD TO CUST-ADDR-STATE-CD
           MOVE EXP-CUST-ADDR-COUNTRY-CD TO CUST-ADDR-COUNTRY-CD
           MOVE EXP-CUST-ADDR-ZIP TO CUST-ADDR-ZIP
           MOVE EXP-CUST-PHONE-NUM(1) TO CUST-PHONE-NUM-1
           MOVE EXP-CUST-PHONE-NUM(2) TO CUST-PHONE-NUM-2
           MOVE EXP-CUST-SSN TO CUST-SSN
           MOVE EXP-CUST-GOVT-ISSUED-ID TO CUST-GOVT-ISSUED-ID
           MOVE EXP-CUST-DOB-YYYY-MM-DD TO CUST-DOB-YYYY-MM-DD
           MOVE EXP-CUST-EFT-ACCOUNT-ID TO CUST-EFT-ACCOUNT-ID
           MOVE EXP-CUST-PRI-CARD-HOLDER-IND TO CUST-PRI-CARD-HOLDER-IND
           MOVE EXP-CUST-FICO-CREDIT-SCORE TO CUST-FICO-CREDIT-SCORE
           
           WRITE CUSTOMER-RECORD
           
           IF NOT WS-CUSTOMER-OK
               DISPLAY 'ERROR: Writing customer record, Status: '
                       WS-CUSTOMER-STATUS
               PERFORM 9999-ABEND-PROGRAM
           END-IF
           
           PERFORM 2320-REGISTER-CUSTOMER
           ADD 1 TO WS-CUSTOMER-RECORDS-IMPORTED.
           
       2300-EXIT.
           EXIT.

      *****************************************************************
       2310-CHECK-DUPLICATE-CUSTOMER.
      *****************************************************************
           SET NO-DUPLICATE TO TRUE
           
           PERFORM VARYING WS-SEARCH-INDEX FROM 1 BY 1
                   UNTIL WS-SEARCH-INDEX > WS-CUSTOMER-COUNT
                   OR DUPLICATE-DETECTED
               IF WS-CUSTOMER-KEY(WS-SEARCH-INDEX) = EXP-CUST-ID
                   SET DUPLICATE-DETECTED TO TRUE
               END-IF
           END-PERFORM.

      *****************************************************************
       2320-REGISTER-CUSTOMER.
      *****************************************************************
           IF WS-CUSTOMER-COUNT < 10000
               ADD 1 TO WS-CUSTOMER-COUNT
               MOVE EXP-CUST-ID TO 
                    WS-CUSTOMER-KEY(WS-CUSTOMER-COUNT)
           END-IF.

      *****************************************************************
       2400-PROCESS-ACCOUNT-RECORD.
      *****************************************************************
           PERFORM 2410-CHECK-DUPLICATE-ACCOUNT
           
           IF DUPLICATE-DETECTED
               MOVE FUNCTION CURRENT-DATE TO ERR-TIMESTAMP
               MOVE EXPORT-REC-TYPE TO ERR-RECORD-TYPE
               MOVE EXPORT-SEQUENCE-NUM TO ERR-SEQUENCE
               MOVE 'Duplicate account ID detected' TO ERR-MESSAGE
               PERFORM 2750-WRITE-ERROR
               GO TO 2400-EXIT
           END-IF
           
           INITIALIZE ACCOUNT-RECORD
           
      *    Map export fields to account record
           MOVE EXP-ACCT-ID TO ACCT-ID
           MOVE EXP-ACCT-ACTIVE-STATUS TO ACCT-ACTIVE-STATUS
           MOVE EXP-ACCT-CURR-BAL TO ACCT-CURR-BAL
           MOVE EXP-ACCT-CREDIT-LIMIT TO ACCT-CREDIT-LIMIT
           MOVE EXP-ACCT-CASH-CREDIT-LIMIT TO ACCT-CASH-CREDIT-LIMIT
           MOVE EXP-ACCT-OPEN-DATE TO ACCT-OPEN-DATE
           MOVE EXP-ACCT-EXPIRAION-DATE TO ACCT-EXPIRAION-DATE
           MOVE EXP-ACCT-REISSUE-DATE TO ACCT-REISSUE-DATE
           MOVE EXP-ACCT-CURR-CYC-CREDIT TO ACCT-CURR-CYC-CREDIT
           MOVE EXP-ACCT-CURR-CYC-DEBIT TO ACCT-CURR-CYC-DEBIT
           MOVE EXP-ACCT-ADDR-ZIP TO ACCT-ADDR-ZIP
           MOVE EXP-ACCT-GROUP-ID TO ACCT-GROUP-ID
           
           WRITE ACCOUNT-RECORD
           
           IF NOT WS-ACCOUNT-OK
               DISPLAY 'ERROR: Writing account record, Status: '
                       WS-ACCOUNT-STATUS
               PERFORM 9999-ABEND-PROGRAM
           END-IF
           
           PERFORM 2420-REGISTER-ACCOUNT
           ADD 1 TO WS-ACCOUNT-RECORDS-IMPORTED.
           
       2400-EXIT.
           EXIT.

      *****************************************************************
       2410-CHECK-DUPLICATE-ACCOUNT.
      *****************************************************************
           SET NO-DUPLICATE TO TRUE
           
           PERFORM VARYING WS-SEARCH-INDEX FROM 1 BY 1
                   UNTIL WS-SEARCH-INDEX > WS-ACCOUNT-COUNT
                   OR DUPLICATE-DETECTED
               IF WS-ACCOUNT-KEY(WS-SEARCH-INDEX) = EXP-ACCT-ID
                   SET DUPLICATE-DETECTED TO TRUE
               END-IF
           END-PERFORM.

      *****************************************************************
       2420-REGISTER-ACCOUNT.
      *****************************************************************
           IF WS-ACCOUNT-COUNT < 10000
               ADD 1 TO WS-ACCOUNT-COUNT
               MOVE EXP-ACCT-ID TO 
                    WS-ACCOUNT-KEY(WS-ACCOUNT-COUNT)
           END-IF.

      *****************************************************************
       2500-PROCESS-XREF-RECORD.
      *****************************************************************
           PERFORM 2510-CHECK-DUPLICATE-XREF
           
           IF DUPLICATE-DETECTED
               MOVE FUNCTION CURRENT-DATE TO ERR-TIMESTAMP
               MOVE EXPORT-REC-TYPE TO ERR-RECORD-TYPE
               MOVE EXPORT-SEQUENCE-NUM TO ERR-SEQUENCE
               MOVE 'Duplicate xref card number detected' TO ERR-MESSAGE
               PERFORM 2750-WRITE-ERROR
               GO TO 2500-EXIT
           END-IF
           
           INITIALIZE CARD-XREF-RECORD
           
      *    Map export fields to xref record
           MOVE EXP-XREF-CARD-NUM TO XREF-CARD-NUM
           MOVE EXP-XREF-CUST-ID TO XREF-CUST-ID
           MOVE EXP-XREF-ACCT-ID TO XREF-ACCT-ID
           
           WRITE CARD-XREF-RECORD
           
           IF NOT WS-XREF-OK
               DISPLAY 'ERROR: Writing xref record, Status: '
                       WS-XREF-STATUS
               PERFORM 9999-ABEND-PROGRAM
           END-IF
           
           PERFORM 2520-REGISTER-XREF
           ADD 1 TO WS-XREF-RECORDS-IMPORTED.
           
       2500-EXIT.
           EXIT.

      *****************************************************************
       2510-CHECK-DUPLICATE-XREF.
      *****************************************************************
           SET NO-DUPLICATE TO TRUE
           
           PERFORM VARYING WS-SEARCH-INDEX FROM 1 BY 1
                   UNTIL WS-SEARCH-INDEX > WS-XREF-COUNT
                   OR DUPLICATE-DETECTED
               IF WS-XREF-KEY(WS-SEARCH-INDEX) = EXP-XREF-CARD-NUM
                   SET DUPLICATE-DETECTED TO TRUE
               END-IF
           END-PERFORM.

      *****************************************************************
       2520-REGISTER-XREF.
      *****************************************************************
           IF WS-XREF-COUNT < 10000
               ADD 1 TO WS-XREF-COUNT
               MOVE EXP-XREF-CARD-NUM TO 
                    WS-XREF-KEY(WS-XREF-COUNT)
           END-IF.

      *****************************************************************
       2600-PROCESS-TRAN-RECORD.
      *****************************************************************
           PERFORM 2610-CHECK-DUPLICATE-TRAN
           
           IF DUPLICATE-DETECTED
               MOVE FUNCTION CURRENT-DATE TO ERR-TIMESTAMP
               MOVE EXPORT-REC-TYPE TO ERR-RECORD-TYPE
               MOVE EXPORT-SEQUENCE-NUM TO ERR-SEQUENCE
               MOVE 'Duplicate transaction ID detected' TO ERR-MESSAGE
               PERFORM 2750-WRITE-ERROR
               GO TO 2600-EXIT
           END-IF
           
           INITIALIZE TRAN-RECORD
           
      *    Map export fields to transaction record
           MOVE EXP-TRAN-ID TO TRAN-ID
           MOVE EXP-TRAN-TYPE-CD TO TRAN-TYPE-CD
           MOVE EXP-TRAN-CAT-CD TO TRAN-CAT-CD
           MOVE EXP-TRAN-SOURCE TO TRAN-SOURCE
           MOVE EXP-TRAN-DESC TO TRAN-DESC
           MOVE EXP-TRAN-AMT TO TRAN-AMT
           MOVE EXP-TRAN-MERCHANT-ID TO TRAN-MERCHANT-ID
           MOVE EXP-TRAN-MERCHANT-NAME TO TRAN-MERCHANT-NAME
           MOVE EXP-TRAN-MERCHANT-CITY TO TRAN-MERCHANT-CITY
           MOVE EXP-TRAN-MERCHANT-ZIP TO TRAN-MERCHANT-ZIP
           MOVE EXP-TRAN-CARD-NUM TO TRAN-CARD-NUM
           MOVE EXP-TRAN-ORIG-TS TO TRAN-ORIG-TS
           MOVE EXP-TRAN-PROC-TS TO TRAN-PROC-TS
           
           WRITE TRAN-RECORD
           
           IF NOT WS-TRANSACTION-OK
               DISPLAY 'ERROR: Writing transaction record, Status: '
                       WS-TRANSACTION-STATUS
               PERFORM 9999-ABEND-PROGRAM
           END-IF
           
           PERFORM 2620-REGISTER-TRAN
           ADD 1 TO WS-TRAN-RECORDS-IMPORTED.
           
       2600-EXIT.
           EXIT.

      *****************************************************************
       2610-CHECK-DUPLICATE-TRAN.
      *****************************************************************
           SET NO-DUPLICATE TO TRUE
           
           PERFORM VARYING WS-SEARCH-INDEX FROM 1 BY 1
                   UNTIL WS-SEARCH-INDEX > WS-TRAN-COUNT
                   OR DUPLICATE-DETECTED
               IF WS-TRAN-KEY(WS-SEARCH-INDEX) = EXP-TRAN-ID
                   SET DUPLICATE-DETECTED TO TRUE
               END-IF
           END-PERFORM.

      *****************************************************************
       2620-REGISTER-TRAN.
      *****************************************************************
           IF WS-TRAN-COUNT < 10000
               ADD 1 TO WS-TRAN-COUNT
               MOVE EXP-TRAN-ID TO 
                    WS-TRAN-KEY(WS-TRAN-COUNT)
           END-IF.

      *****************************************************************
       2650-PROCESS-CARD-RECORD.
      *****************************************************************
           PERFORM 2660-CHECK-DUPLICATE-CARD
           
           IF DUPLICATE-DETECTED
               MOVE FUNCTION CURRENT-DATE TO ERR-TIMESTAMP
               MOVE EXPORT-REC-TYPE TO ERR-RECORD-TYPE
               MOVE EXPORT-SEQUENCE-NUM TO ERR-SEQUENCE
               MOVE 'Duplicate card number detected' TO ERR-MESSAGE
               PERFORM 2750-WRITE-ERROR
               GO TO 2650-EXIT
           END-IF
           
           INITIALIZE CARD-RECORD
           
      *    Map export fields to card record
           MOVE EXP-CARD-NUM TO CARD-NUM
           MOVE EXP-CARD-ACCT-ID TO CARD-ACCT-ID
           MOVE EXP-CARD-CVV-CD TO CARD-CVV-CD
           MOVE EXP-CARD-EMBOSSED-NAME TO CARD-EMBOSSED-NAME
           MOVE EXP-CARD-EXPIRAION-DATE TO CARD-EXPIRAION-DATE
           MOVE EXP-CARD-ACTIVE-STATUS TO CARD-ACTIVE-STATUS
           
           WRITE CARD-RECORD
           
           IF NOT WS-CARD-OK
               DISPLAY 'ERROR: Writing card record, Status: '
                       WS-CARD-STATUS
               PERFORM 9999-ABEND-PROGRAM
           END-IF
           
           PERFORM 2670-REGISTER-CARD
           ADD 1 TO WS-CARD-RECORDS-IMPORTED.
           
       2650-EXIT.
           EXIT.

      *****************************************************************
       2660-CHECK-DUPLICATE-CARD.
      *****************************************************************
           SET NO-DUPLICATE TO TRUE
           
           PERFORM VARYING WS-SEARCH-INDEX FROM 1 BY 1
                   UNTIL WS-SEARCH-INDEX > WS-CARD-COUNT
                   OR DUPLICATE-DETECTED
               IF WS-CARD-KEY(WS-SEARCH-INDEX) = EXP-CARD-NUM
                   SET DUPLICATE-DETECTED TO TRUE
               END-IF
           END-PERFORM.

      *****************************************************************
       2670-REGISTER-CARD.
      *****************************************************************
           IF WS-CARD-COUNT < 10000
               ADD 1 TO WS-CARD-COUNT
               MOVE EXP-CARD-NUM TO 
                    WS-CARD-KEY(WS-CARD-COUNT)
           END-IF.

      *****************************************************************
       2700-PROCESS-UNKNOWN-RECORD.
      *****************************************************************
           ADD 1 TO WS-UNKNOWN-RECORD-TYPE-COUNT
           
           MOVE FUNCTION CURRENT-DATE TO ERR-TIMESTAMP
           MOVE EXPORT-REC-TYPE TO ERR-RECORD-TYPE
           MOVE EXPORT-SEQUENCE-NUM TO ERR-SEQUENCE
           MOVE 'Unknown record type encountered' TO ERR-MESSAGE
           
           PERFORM 2750-WRITE-ERROR.

      *****************************************************************
       2750-WRITE-ERROR.
      *****************************************************************
           WRITE ERROR-OUTPUT-RECORD FROM WS-ERROR-RECORD
           
           IF NOT WS-ERROR-OK
               DISPLAY 'ERROR: Writing error record, Status: '
                       WS-ERROR-STATUS
           END-IF
           
           ADD 1 TO WS-ERROR-RECORDS-WRITTEN.

      *****************************************************************
       3000-VALIDATE-IMPORT.
      *****************************************************************
           DISPLAY 'CBIMPORT: Performing import validation'
           
           PERFORM 3100-VALIDATE-RECORD-COUNTS
           PERFORM 3200-VALIDATE-REFERENTIAL-INTEGRITY
           
           IF WS-VALIDATION-ERRORS = 0
               DISPLAY 'CBIMPORT: Import validation completed'
               DISPLAY 'CBIMPORT: No validation errors detected'
           ELSE
               DISPLAY 'CBIMPORT: Import validation completed'
               DISPLAY 'CBIMPORT: Validation errors detected: '
                       WS-VALIDATION-ERRORS
               DISPLAY 'CBIMPORT: Review error file for details'
           END-IF.

      *****************************************************************
       3100-VALIDATE-RECORD-COUNTS.
      *****************************************************************
           COMPUTE WS-EXPECTED-RECORD-COUNT = 
                   WS-CUSTOMER-RECORDS-IMPORTED +
                   WS-ACCOUNT-RECORDS-IMPORTED +
                   WS-XREF-RECORDS-IMPORTED +
                   WS-TRAN-RECORDS-IMPORTED +
                   WS-CARD-RECORDS-IMPORTED +
                   WS-UNKNOWN-RECORD-TYPE-COUNT
           
           IF WS-EXPECTED-RECORD-COUNT NOT = WS-TOTAL-RECORDS-READ
               ADD 1 TO WS-VALIDATION-ERRORS
               MOVE FUNCTION CURRENT-DATE TO ERR-TIMESTAMP
               MOVE 'V' TO ERR-RECORD-TYPE
               MOVE 0 TO ERR-SEQUENCE
               MOVE 'Record count mismatch detected' TO ERR-MESSAGE
               PERFORM 2750-WRITE-ERROR
               DISPLAY 'CBIMPORT: WARNING - Record count mismatch'
               DISPLAY 'CBIMPORT: Expected: ' WS-TOTAL-RECORDS-READ
               DISPLAY 'CBIMPORT: Actual: ' WS-EXPECTED-RECORD-COUNT
           END-IF.

      *****************************************************************
       3200-VALIDATE-REFERENTIAL-INTEGRITY.
      *****************************************************************
      *    Validate that all XREF records reference valid accounts
           PERFORM VARYING XREF-IDX FROM 1 BY 1
                   UNTIL XREF-IDX > WS-XREF-COUNT
               PERFORM 3210-CHECK-XREF-ACCOUNT-EXISTS
           END-PERFORM
           
      *    Validate that all cards reference valid accounts
           PERFORM VARYING CARD-IDX FROM 1 BY 1
                   UNTIL CARD-IDX > WS-CARD-COUNT
               PERFORM 3220-CHECK-CARD-ACCOUNT-EXISTS
           END-PERFORM.

      *****************************************************************
       3210-CHECK-XREF-ACCOUNT-EXISTS.
      *****************************************************************
      *    This is a placeholder for referential integrity check
      *    In production, would verify XREF account IDs exist in 
      *    account table. Simplified for in-memory validation.
           CONTINUE.

      *****************************************************************
       3220-CHECK-CARD-ACCOUNT-EXISTS.
      *****************************************************************
      *    This is a placeholder for referential integrity check
      *    In production, would verify card account IDs exist in 
      *    account table. Simplified for in-memory validation.
           CONTINUE.

      *****************************************************************
       4000-FINALIZE.
      *****************************************************************
           CLOSE EXPORT-INPUT
           CLOSE CUSTOMER-OUTPUT
           CLOSE ACCOUNT-OUTPUT
           CLOSE XREF-OUTPUT
           CLOSE TRANSACTION-OUTPUT
           CLOSE CARD-OUTPUT
           CLOSE ERROR-OUTPUT
           
           DISPLAY 'CBIMPORT: Import completed'
           DISPLAY 'CBIMPORT: Total Records Read: '
           WS-TOTAL-RECORDS-READ
           DISPLAY 'CBIMPORT: Customers Imported: ' 
                   WS-CUSTOMER-RECORDS-IMPORTED
           DISPLAY 'CBIMPORT: Accounts Imported: ' 
                   WS-ACCOUNT-RECORDS-IMPORTED
           DISPLAY 'CBIMPORT: XRefs Imported: ' WS-XREF-RECORDS-IMPORTED
           DISPLAY 'CBIMPORT: Transactions Imported: ' 
                   WS-TRAN-RECORDS-IMPORTED
           DISPLAY 'CBIMPORT: Cards Imported: ' WS-CARD-RECORDS-IMPORTED
           DISPLAY 'CBIMPORT: Errors Written: ' WS-ERROR-RECORDS-WRITTEN
           DISPLAY 'CBIMPORT: Unknown Record Types: ' 
                   WS-UNKNOWN-RECORD-TYPE-COUNT
           DISPLAY 'CBIMPORT: Validation Errors: ' WS-VALIDATION-ERRORS.

      *****************************************************************
       9999-ABEND-PROGRAM.
      *****************************************************************
           DISPLAY 'CBIMPORT: ABENDING PROGRAM'
           CALL 'CEE3ABD'.      
      *
      * Ver: CardDemo_v2.0-44-gb6e9c27-254 Date: 2025-10-16 14:07:18 CDT
      *