package com.grapeup.carddemo.onlinetransactionmanagement.domain;

import jakarta.persistence.Column;
import jakarta.persistence.Embedded;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;

/** Links a card number to its owning account and customer, enabling card-to-account lookup */
@Entity
@Table(name = "card_cross_references")
public class CardCrossReference {

    @Id
    @Column(name = "card_number", length = 16)
    private String cardNumber;

    @Column(name = "customer_id")
    private Integer customerId;

    /** Each card is issued against exactly one account. The cross-reference table owns this FK because it is the many side (multiple cards per account) */
    @Column(name = "account_id")
    private Long accountId;

    public String getCardNumber() {
        return cardNumber;
    }

    public void setCardNumber(String cardNumber) {
        this.cardNumber = cardNumber;
    }

    public Integer getCustomerId() {
        return customerId;
    }

    public void setCustomerId(Integer customerId) {
        this.customerId = customerId;
    }

    public Long getAccountId() {
        return accountId;
    }

    public void setAccountId(Long accountId) {
        this.accountId = accountId;
    }
}