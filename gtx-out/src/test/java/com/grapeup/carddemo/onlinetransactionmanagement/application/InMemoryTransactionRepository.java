package com.grapeup.carddemo.onlinetransactionmanagement.application;

import com.grapeup.carddemo.onlinetransactionmanagement.domain.Transaction;
import com.grapeup.carddemo.onlinetransactionmanagement.domain.Merchant;

import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Optional;

/**
 * In-memory fake for {@link TransactionRepository}.
 */
class InMemoryTransactionRepository implements TransactionRepository {

    private final Map<String, Transaction> store = new LinkedHashMap<>();

    void seed(Transaction txn) {
        store.put(txn.getId(), copy(txn));
    }

    @Override
    public Optional<String> findMaxTransactionId() {
        return store.keySet().stream().max(Comparator.naturalOrder());
    }

    @Override
    public void save(Transaction txn) {
        store.put(txn.getId(), copy(txn));
    }

    Transaction getStored(String id) {
        Transaction t = store.get(id);
        return t == null ? null : copy(t);
    }

    int size() {
        return store.size();
    }

    private Transaction copy(Transaction src) {
        Transaction dst = new Transaction();
        dst.setId(src.getId());
        dst.setTypeCode(src.getTypeCode());
        dst.setCategoryCode(src.getCategoryCode());
        dst.setSource(src.getSource());
        dst.setDescription(src.getDescription());
        dst.setAmount(src.getAmount());
        dst.setOriginatedAt(src.getOriginatedAt());
        dst.setProcessedAt(src.getProcessedAt());
        dst.setCardNumber(src.getCardNumber());
        if (src.getMerchant() != null) {
            Merchant m = new Merchant();
            m.setId(src.getMerchant().getId());
            m.setName(src.getMerchant().getName());
            m.setCity(src.getMerchant().getCity());
            m.setZip(src.getMerchant().getZip());
            dst.setMerchant(m);
        }
        return dst;
    }
}
