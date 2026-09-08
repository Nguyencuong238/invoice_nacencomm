/**
 * Invoice Manager UI Logic
 * Max 50 selections, AJAX filtering, Tab switching, Modal updates
 */
document.addEventListener('DOMContentLoaded', function () {
    initInvoiceEvents();
    checkSignerConnection();
});

function checkSignerConnection() {
    WinFormsSigner.checkStatus().then(status => {
        const statusBadge = document.getElementById('signerStatusBadge');
        if (statusBadge) {
            if (status.isRunning) {
                statusBadge.className = 'badge bg-success';
                statusBadge.innerHTML = `<i class="bi bi-shield-check me-1"></i> WinForms Signer (v${status.version || '2.0'}) - Sẵn sàng`;
            } else {
                statusBadge.className = 'badge bg-warning text-dark';
                statusBadge.innerHTML = `<i class="bi bi-exclamation-triangle me-1"></i> WinForms Signer - Chưa kết nối (127.0.0.1:8765)`;
            }
        }
    });
}

function initInvoiceEvents() {
    const selectAllCheckbox = document.getElementById('selectAllInvoices');
    const rowCheckboxes = document.querySelectorAll('.invoice-checkbox');
    const selectedCountSpan = document.getElementById('selectedInvoiceCount');
    const btnBatchSign = document.getElementById('btnBatchSign');

    function updateSelectionState() {
        const checkedBoxes = document.querySelectorAll('.invoice-checkbox:checked');
        const count = checkedBoxes.length;

        if (selectedCountSpan) {
            selectedCountSpan.textContent = `${count} / 50`;
        }

        if (btnBatchSign) {
            if (count > 0 && count <= 50) {
                btnBatchSign.disabled = false;
                btnBatchSign.classList.remove('btn-secondary', 'btn-primary');
                btnBatchSign.classList.add('btn-teal-sign');
            } else {
                btnBatchSign.disabled = true;
                btnBatchSign.classList.remove('btn-teal-sign', 'btn-primary');
                btnBatchSign.classList.add('btn-secondary');
            }
        }

        // Update Select All Checkbox state
        if (selectAllCheckbox) {
            const enabledBoxes = document.querySelectorAll('.invoice-checkbox:not(:disabled)');
            if (enabledBoxes.length > 0 && checkedBoxes.length === Math.min(enabledBoxes.length, 50)) {
                selectAllCheckbox.checked = true;
                selectAllCheckbox.indeterminate = false;
            } else if (checkedBoxes.length > 0) {
                selectAllCheckbox.checked = false;
                selectAllCheckbox.indeterminate = true;
            } else {
                selectAllCheckbox.checked = false;
                selectAllCheckbox.indeterminate = false;
            }
        }
    }

    // Individual checkbox click event
    rowCheckboxes.forEach(cb => {
        cb.addEventListener('change', function () {
            const checkedBoxes = document.querySelectorAll('.invoice-checkbox:checked');
            if (checkedBoxes.length > 50) {
                this.checked = false;
                alert("Chú ý: Một lượt ký chỉ cho phép chọn tối đa 50 hóa đơn!");
            }
            updateSelectionState();
        });
    });

    // Select All event (limit to max 50)
    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function () {
            const enabledBoxes = Array.from(document.querySelectorAll('.invoice-checkbox:not(:disabled)'));
            const isChecking = this.checked;

            let checkedCount = 0;
            enabledBoxes.forEach(cb => {
                if (isChecking && checkedCount < 50) {
                    cb.checked = true;
                    checkedCount++;
                } else {
                    cb.checked = false;
                }
            });

            if (isChecking && enabledBoxes.length > 50) {
                alert("Chú ý: Danh sách có hơn 50 hóa đơn, hệ thống tự động chọn 50 hóa đơn đầu tiên (sắp xếp theo Ngày xuất ASC) theo đúng quy chuẩn kế hoạch!");
            }

            updateSelectionState();
        });
    }

    // Tab status click handler
    const tabLinks = document.querySelectorAll('.nav-tab-status');
    tabLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            tabLinks.forEach(t => t.classList.remove('active', 'fw-bold', 'text-teal', 'border-bottom', 'border-3', 'border-teal'));
            this.classList.add('active', 'fw-bold', 'text-teal', 'border-bottom', 'border-3', 'border-teal');

            const tabValue = this.getAttribute('data-tab');
            document.getElementById('hiddenTabStatus').value = tabValue;

            const posSubTabsContainer = document.getElementById('posSubTabsContainer');
            if (posSubTabsContainer) {
                if (tabValue === 'POS') {
                    posSubTabsContainer.classList.remove('d-none');
                } else {
                    posSubTabsContainer.classList.add('d-none');
                }
            }

            filterInvoicesAjax();
        });
    });

    // Subtab click handler for POS tab
    const subtabBtns = document.querySelectorAll('.pos-subtab-btn');
    subtabBtns.forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            subtabBtns.forEach(b => {
                b.classList.remove('btn-primary', 'text-white');
                b.classList.add('btn-outline-primary', 'bg-white');
            });
            this.classList.remove('btn-outline-primary', 'bg-white');
            this.classList.add('btn-primary', 'text-white');

            const subtabVal = this.getAttribute('data-subtab');
            document.getElementById('hiddenSubTabStatus').value = subtabVal;
            filterInvoicesAjax();
        });
    });

    // Batch Sign Button click handler
    if (btnBatchSign) {
        btnBatchSign.addEventListener('click', function () {
            const checkedBoxes = document.querySelectorAll('.invoice-checkbox:checked');
            const selectedIds = Array.from(checkedBoxes).map(cb => parseInt(cb.value));

            if (selectedIds.length === 0) {
                alert("Vui lòng chọn ít nhất 1 hóa đơn để ký.");
                return;
            }

            if (selectedIds.length > 50) {
                alert("Tối đa 50 hóa đơn cho 1 đợt ký.");
                return;
            }

            openSigningModal(selectedIds);
        });
    }

    updateSelectionState();
}

/**
 * Filter table rows dynamically client-side based on inline column inputs
 */
function applyInlineColumnFilter() {
    const invNumVal = (document.getElementById('filterColInvNum')?.value || '').trim().toLowerCase();
    const buyerVal = (document.getElementById('filterColBuyer')?.value || '').trim().toLowerCase();
    const lookupVal = (document.getElementById('filterColLookup')?.value || '').trim().toLowerCase();
    const cqtVal = (document.getElementById('filterColCqt')?.value || '').trim().toLowerCase();

    const dataRows = document.querySelectorAll('.invoice-data-row');
    dataRows.forEach(row => {
        const rowInvNum = (row.getAttribute('data-invnum') || '').toLowerCase();
        const rowBuyer = (row.getAttribute('data-buyer') || '').toLowerCase();
        const rowLookup = (row.getAttribute('data-lookup') || '').toLowerCase();
        const rowCqt = (row.getAttribute('data-cqt') || '').toLowerCase();

        const matchInvNum = !invNumVal || rowInvNum.includes(invNumVal);
        const matchBuyer = !buyerVal || rowBuyer.includes(buyerVal);
        const matchLookup = !lookupVal || rowLookup.includes(lookupVal);
        const matchCqt = !cqtVal || rowCqt.includes(cqtVal);

        const subRow = row.nextElementSibling && row.nextElementSibling.classList.contains('invoice-sub-row') ? row.nextElementSibling : null;

        if (matchInvNum && matchBuyer && matchLookup && matchCqt) {
            row.style.display = '';
            if (subRow) subRow.style.display = '';
        } else {
            row.style.display = 'none';
            if (subRow) subRow.style.display = 'none';
        }
    });
}

function triggerSingleSign(invoiceId) {
    if (confirm(`Bạn có muốn khởi tạo tiến trình ký số cho hóa đơn #${invoiceId} không?`)) {
        openSigningModal([invoiceId]);
    }
}

function filterInvoicesAjax() {
    const form = document.getElementById('invoiceFilterForm');
    if (!form) return;

    const formData = new FormData(form);
    const searchParams = new URLSearchParams(formData);

    const tableContainer = document.getElementById('invoiceTableContainer');
    if (tableContainer) {
        tableContainer.style.opacity = '0.5';
    }

    fetch(`/Invoice/GetInvoicesPartial?${searchParams.toString()}`)
        .then(res => res.text())
        .then(html => {
            if (tableContainer) {
                tableContainer.innerHTML = html;
                tableContainer.style.opacity = '1';
                initInvoiceEvents();
            }
        })
        .catch(err => {
            console.error("Lỗi tải lại danh sách hóa đơn:", err);
            if (tableContainer) tableContainer.style.opacity = '1';
        });
}

/**
 * Trigger Excel Export using current filter parameters
 */
function exportInvoicesExcel() {
    const form = document.getElementById('invoiceFilterForm');
    let searchParams = '';
    if (form) {
        const formData = new FormData(form);
        searchParams = new URLSearchParams(formData).toString();
    }
    window.location.href = `/Invoice/ExportExcel?${searchParams}`;
}

/**
 * Open Signing Progress Modal and start queue
 */
function openSigningModal(selectedIds) {
    const modalElem = document.getElementById('signingProgressModal');
    if (!modalElem) return;

    const bsModal = new bootstrap.Modal(modalElem, { backdrop: 'static', keyboard: false });

    // Reset Modal UI
    document.getElementById('modalTotalCount').textContent = selectedIds.length;
    document.getElementById('modalCompletedCount').textContent = '0';
    document.getElementById('modalSuccessCount').textContent = '0';
    document.getElementById('modalFailedCount').textContent = '0';
    document.getElementById('modalProgressBar').style.width = '0%';
    document.getElementById('modalProgressBar').textContent = '0%';
    document.getElementById('modalStatusText').textContent = 'Đang khởi tạo Job ký...';
    document.getElementById('btnCancelSigning').disabled = false;
    document.getElementById('btnCloseSigningModal').disabled = true;

    const itemsTbody = document.getElementById('signingModalItemsTbody');
    itemsTbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted py-3"><div class="spinner-border spinner-border-sm me-2"></div>Đang tải thông tin chi tiết hóa đơn...</td></tr>`;

    bsModal.show();

    // Start Batch Signing Queue
    SigningQueue.startJob(selectedIds, {
        onJobCreated: function (job) {
            renderModalItemsTable(job.items);
            document.getElementById('modalStatusText').textContent = `Đang xử lý ký ${job.total} hóa đơn (Sắp xếp theo Ngày xuất ASC)...`;
        },
        onItemStatusChange: function (item, job) {
            updateModalProgressUI(item, job);
        },
        onFinished: function (job, isCompleted, errorCode) {
            document.getElementById('btnCancelSigning').disabled = true;
            document.getElementById('btnCloseSigningModal').disabled = false;

            if (isCompleted) {
                document.getElementById('modalStatusText').innerHTML = `<span class="text-success"><i class="bi bi-check-circle-fill me-1"></i> Hoàn thành tiến trình ký! Thành công ${job.success}/${job.total}.</span>`;
            } else if (errorCode === "SIGNER_NOT_RUNNING") {
                document.getElementById('modalStatusText').innerHTML = `<span class="text-danger"><i class="bi bi-x-circle-fill me-1"></i> Lỗi: Ứng dụng WinForms Signer không hoạt động.</span>`;
            } else {
                document.getElementById('modalStatusText').innerHTML = `<span class="text-warning"><i class="bi bi-exclamation-triangle-fill me-1"></i> Tiến trình đã bị hủy. Đã xử lý ${job.completed}/${job.total}.</span>`;
            }

            // Auto refresh main list behind modal
            filterInvoicesAjax();
        }
    });

    document.getElementById('btnCancelSigning').onclick = function () {
        if (confirm("Bạn có chắc chắn muốn hủy tiến trình ký số không? Hóa đơn đang ký dở sẽ hoàn tất, các hóa đơn chưa ký sẽ bị dừng.")) {
            SigningQueue.cancel();
            this.disabled = true;
            document.getElementById('modalStatusText').textContent = "Đang dừng tiến trình ký...";
        }
    };
}

function renderModalItemsTable(items) {
    const tbody = document.getElementById('signingModalItemsTbody');
    if (!tbody) return;

    tbody.innerHTML = items.map((item, idx) => `
        <tr id="modal-row-${item.invoiceId}">
            <td>${idx + 1}</td>
            <td class="fw-bold">${item.invoiceNumber}</td>
            <td>${new Date(item.invoiceDate).toLocaleDateString('vi-VN')}</td>
            <td><span class="badge bg-secondary item-status-badge">Đang chờ</span></td>
            <td class="item-detail-cell text-muted">-</td>
        </tr>
    `).join('');
}

function updateModalProgressUI(item, job) {
    document.getElementById('modalCompletedCount').textContent = job.completed;
    document.getElementById('modalSuccessCount').textContent = job.success;
    document.getElementById('modalFailedCount').textContent = job.failed;

    const percent = Math.round((job.completed / job.total) * 100);
    const progressBar = document.getElementById('modalProgressBar');
    progressBar.style.width = `${percent}%`;
    progressBar.textContent = `${percent}%`;

    const row = document.getElementById(`modal-row-${item.invoiceId}`);
    if (row) {
        const badge = row.querySelector('.item-status-badge');
        const detailCell = row.querySelector('.item-detail-cell');

        if (item.status === 'GettingXml') {
            badge.className = 'badge bg-info text-dark';
            badge.textContent = 'Đang lấy XML';
        } else if (item.status === 'Signing') {
            badge.className = 'badge bg-primary';
            badge.textContent = 'Đang ký WinForms';
        } else if (item.status === 'Submitting') {
            badge.className = 'badge bg-warning text-dark';
            badge.textContent = 'Đang cập nhật';
        } else if (item.status === 'Success') {
            badge.className = 'badge bg-success';
            badge.textContent = 'Thành công';
            detailCell.className = 'item-detail-cell text-success';
            detailCell.textContent = 'Ký & Cập nhật thành công';
        } else if (item.status === 'Failed') {
            badge.className = 'badge bg-danger';
            badge.textContent = 'Thất bại';
            detailCell.className = 'item-detail-cell text-danger';
            detailCell.textContent = item.errorMessage || item.errorCode || 'Lỗi ký';
        }
    }
}
