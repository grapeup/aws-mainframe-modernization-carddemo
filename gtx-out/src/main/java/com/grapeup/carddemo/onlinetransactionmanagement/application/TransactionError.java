package com.grapeup.carddemo.onlinetransactionmanagement.application;

/**
 * Business error codes for online transaction management.
 * Each member carries the original legacy message text in its Javadoc
 * so parity can be verified without coupling tests to string literals.
 */
public enum TransactionError {

    /** "Acct ID can NOT be empty" */
    ACCOUNT_ID_MISSING,

    /** "Account ID NOT found" */
    ACCOUNT_NOT_FOUND,

    /** "Account is not active" */
    ACCOUNT_NOT_ACTIVE,

    /** "You have nothing to pay" */
    NOTHING_TO_PAY,

    /** "Confirm to make a bill payment" */
    PAYMENT_NOT_CONFIRMED,

    /** "Invalid amount format" */
    INVALID_AMOUNT_FORMAT,

    /** "Card number not found" */
    CARD_NOT_FOUND
}
