package com.grapeup.carddemo.onlinetransactionmanagement.application;

import com.grapeup.carddemo.onlinetransactionmanagement.domain.Account;

import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Optional;

/**
 * In-memory fake for {@link AccountRepository}.
 * Stores accounts by ID. Save stages the account; the unit of work controls visibility.
 */
class InMemoryAccountRepository implements AccountRepository {

    private final Map<Long, Account> store = new LinkedHashMap<>();

    void seed(Account account) {
        store.put(account.getId(), copy(account));
    }

    @Override
    public Optional<Account> findById(Long id) {
        Account a = store.get(id);
        return a == null ? Optional.empty() : Optional.of(copy(a));
    }

    @Override
    public void save(Account account) {
        store.put(account.getId(), copy(account));
    }

    Account getStored(Long id) {
        Account a = store.get(id);
        return a == null ? null : copy(a);
    }

    private Account copy(Account src) {
        Account dst = new Account();
        dst.setId(src.getId());
        dst.setActiveStatus(src.getActiveStatus());
        dst.setCurrentBalance(src.getCurrentBalance());
        dst.setCreditLimit(src.getCreditLimit());
        dst.setCashCreditLimit(src.getCashCreditLimit());
        dst.setOpenDate(src.getOpenDate());
        dst.setExpirationDate(src.getExpirationDate());
        dst.setReissueDate(src.getReissueDate());
        dst.setCurrentCycleCredit(src.getCurrentCycleCredit());
        dst.setCurrentCycleDebit(src.getCurrentCycleDebit());
        dst.setAddressZip(src.getAddressZip());
        dst.setGroupId(src.getGroupId());
        return dst;
    }
}
