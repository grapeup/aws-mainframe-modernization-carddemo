package com.grapeup.carddemo.onlinetransactionmanagement.application;

/**
 * Explicit transaction boundary.
 * <p>
 * The service calls {@link #begin()} before any read that must be serialised
 * with a later write (e.g. reading the max transaction ID before inserting a new one).
 * {@link #commit()} makes all staged repository changes durable.
 * {@link #rollback()} discards them.
 * </p>
 */
public interface UnitOfWork {

    /**
     * Opens the transaction. Any read that must be serialised with a later write
     * has to happen inside it, so the service calls this first.
     */
    void begin();

    /** Makes all staged changes durable. */
    void commit();

    /** Discards all staged changes. */
    void rollback();
}
