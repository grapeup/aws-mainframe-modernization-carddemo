package com.grapeup.carddemo.onlinetransactionmanagement.application;

/**
 * In-memory fake for {@link UnitOfWork}.
 * Tracks whether begin/commit/rollback were called so tests can assert
 * on transactional behaviour.
 */
class InMemoryUnitOfWork implements UnitOfWork {

    private boolean begun = false;
    private boolean committed = false;
    private boolean rolledBack = false;

    @Override
    public void begin() {
        begun = true;
        committed = false;
        rolledBack = false;
    }

    @Override
    public void commit() {
        committed = true;
    }

    @Override
    public void rollback() {
        rolledBack = true;
    }

    boolean wasBegun() {
        return begun;
    }

    boolean wasCommitted() {
        return committed;
    }

    boolean wasRolledBack() {
        return rolledBack;
    }

    void reset() {
        begun = false;
        committed = false;
        rolledBack = false;
    }
}
