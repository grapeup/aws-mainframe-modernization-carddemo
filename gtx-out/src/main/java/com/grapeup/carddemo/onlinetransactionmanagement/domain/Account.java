package com.grapeup.carddemo.onlinetransactionmanagement.domain;

import jakarta.persistence.Column;
import jakarta.persistence.Embedded;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.math.BigDecimal;
import java.time.LocalDate;

/** A customer credit-card account with balance, credit limits, and cycle-to-date totals */
@Entity
@Table(name = "accounts")
public class Account {

    @Id
    @Column(name = "id")
    private Long id;

    /** Single-character status flag checked in business rules (e.g. 'Y' = active) */
    @Column(name = "active_status", length = 1)
    private String activeStatus;

    /** Money - BigDecimal, never double. Checked for positive balance before payment. */
    @Column(name = "current_balance", precision = 12, scale = 2)
    private BigDecimal currentBalance;

    /** Money - BigDecimal, never double */
    @Column(name = "credit_limit", precision = 12, scale = 2)
    private BigDecimal creditLimit;

    /** Money - BigDecimal, never double */
    @Column(name = "cash_credit_limit", precision = 12, scale = 2)
    private BigDecimal cashCreditLimit;

    /** Proven date field, format yyyy-MM-dd */
    @Column(name = "open_date")
    private LocalDate openDate;

    /** Proven date field, format yyyy-MM-dd. Legacy field name has typo (EXPIRAION); Java name corrected. */
    @Column(name = "expiration_date")
    private LocalDate expirationDate;

    /** Proven date field, format yyyy-MM-dd. Nullable because not all accounts have been reissued. */
    @Column(name = "reissue_date")
    private LocalDate reissueDate;

    /** Money - BigDecimal. Updated when credit transactions are added. */
    @Column(name = "current_cycle_credit", precision = 12, scale = 2)
    private BigDecimal currentCycleCredit;

    /** Money - BigDecimal. Updated when debit transactions are added. */
    @Column(name = "current_cycle_debit", precision = 12, scale = 2)
    private BigDecimal currentCycleDebit;

    /** ZIP/postal code - kept as String to preserve leading zeros */
    @Column(name = "address_zip", length = 10)
    private String addressZip;

    /** Account grouping identifier */
    @Column(name = "group_id", length = 10)
    private String groupId;

    public Long getId() {
        return id;
    }

    public void setId(Long id) {
        this.id = id;
    }

    public String getActiveStatus() {
        return activeStatus;
    }

    public void setActiveStatus(String activeStatus) {
        this.activeStatus = activeStatus;
    }

    public BigDecimal getCurrentBalance() {
        return currentBalance;
    }

    public void setCurrentBalance(BigDecimal currentBalance) {
        this.currentBalance = currentBalance;
    }

    public BigDecimal getCreditLimit() {
        return creditLimit;
    }

    public void setCreditLimit(BigDecimal creditLimit) {
        this.creditLimit = creditLimit;
    }

    public BigDecimal getCashCreditLimit() {
        return cashCreditLimit;
    }

    public void setCashCreditLimit(BigDecimal cashCreditLimit) {
        this.cashCreditLimit = cashCreditLimit;
    }

    public LocalDate getOpenDate() {
        return openDate;
    }

    public void setOpenDate(LocalDate openDate) {
        this.openDate = openDate;
    }

    public LocalDate getExpirationDate() {
        return expirationDate;
    }

    public void setExpirationDate(LocalDate expirationDate) {
        this.expirationDate = expirationDate;
    }

    public LocalDate getReissueDate() {
        return reissueDate;
    }

    public void setReissueDate(LocalDate reissueDate) {
        this.reissueDate = reissueDate;
    }

    public BigDecimal getCurrentCycleCredit() {
        return currentCycleCredit;
    }

    public void setCurrentCycleCredit(BigDecimal currentCycleCredit) {
        this.currentCycleCredit = currentCycleCredit;
    }

    public BigDecimal getCurrentCycleDebit() {
        return currentCycleDebit;
    }

    public void setCurrentCycleDebit(BigDecimal currentCycleDebit) {
        this.currentCycleDebit = currentCycleDebit;
    }

    public String getAddressZip() {
        return addressZip;
    }

    public void setAddressZip(String addressZip) {
        this.addressZip = addressZip;
    }

    public String getGroupId() {
        return groupId;
    }

    public void setGroupId(String groupId) {
        this.groupId = groupId;
    }
}