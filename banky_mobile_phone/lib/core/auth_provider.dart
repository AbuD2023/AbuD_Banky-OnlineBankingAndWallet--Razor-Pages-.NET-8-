import 'package:flutter/material.dart';
import '../models/user_model.dart';
import '../models/wallet_model.dart';
import '../services/api_service.dart';

/// مزود حالة المستخدم والمصادقة والمحافظ (Auth Provider)
/// يدير حالة تسجيل الدخول، بيانات الملف الشخصي، المحافظ والأرصدة، وتحديثات الخصوصية
class AuthProvider extends ChangeNotifier {
  UserModel? _user;
  List<WalletModel> _wallets = [];
  WalletModel? _selectedWallet;
  bool _isLoading = false;
  String? _errorMessage;

  UserModel? get user => _user;
  List<WalletModel> get wallets => _wallets;
  WalletModel? get selectedWallet => _selectedWallet;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  /// هل المستخدم مسجل دخوله حالياً
  bool get isAuthenticated => _user != null;

  /// هل الحساب موثق ومقبول من الإدارة
  bool get isKycApproved => _user?.isApproved ?? false;

  /// هل الحساب مجبر على تغيير كلمة المرور
  bool get mustChangePassword => _user?.mustChangePassword ?? false;

  /// تعيين المحفظة النشطة المختارة
  void setSelectedWallet(WalletModel? wallet) {
    _selectedWallet = wallet;
    notifyListeners();
  }

  /// التحقق من حالة تسجيل الدخول عند بدء التطبيق
  Future<bool> checkLoginStatus() async {
    _isLoading = true;
    notifyListeners();

    try {
      final token = await ApiService.getToken();
      if (token == null || token.isEmpty) {
        _isLoading = false;
        notifyListeners();
        return false;
      }

      final profile = await ApiService.getProfile();
      if (profile != null) {
        _user = profile;
        await refreshWallets();
        _isLoading = false;
        notifyListeners();
        return true;
      }

      await ApiService.clearToken();
      _isLoading = false;
      notifyListeners();
      return false;
    } catch (_) {
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  /// تسجيل الدخول
  Future<bool> login(String identifier, String password) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    final result = await ApiService().login(identifier, password);
    _isLoading = false;

    if (result['success'] == true) {
      if (result['data'] != null && result['data']['user'] != null) {
        _user = UserModel.fromJson(result['data']['user']);
      } else {
        _user = await ApiService.getProfile();
      }
      await refreshWallets();
      notifyListeners();
      return true;
    } else {
      _errorMessage = result['message'];
      notifyListeners();
      return false;
    }
  }

  /// تسجيل حساب جديد
  Future<bool> register({
    required String fullName,
    required String email,
    required String phone,
    required String password,
  }) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    final result = await ApiService.register(
      fullName: fullName,
      email: email,
      phone: phone,
      password: password,
    );
    _isLoading = false;

    if (result['success'] == true) {
      if (result['data'] != null && result['data']['user'] != null) {
        _user = UserModel.fromJson(result['data']['user']);
      } else {
        _user = await ApiService.getProfile();
      }
      await refreshWallets();
      notifyListeners();
      return true;
    } else {
      _errorMessage = result['message'];
      notifyListeners();
      return false;
    }
  }

  /// تحديث بيانات الملف الشخصي
  Future<void> refreshProfile() async {
    final profile = await ApiService.getProfile();
    if (profile != null) {
      _user = profile;
      notifyListeners();
    }
  }

  /// تحديث قائمة المحافظ المالية
  Future<void> refreshWallets() async {
    final fetchedWallets = await ApiService.getWallets();
    _wallets = fetchedWallets;

    if (_wallets.isNotEmpty) {
      if (_selectedWallet == null ||
          !_wallets.any((w) => w.id == _selectedWallet!.id)) {
        _selectedWallet = _wallets.first;
      } else {
        _selectedWallet = _wallets.firstWhere(
          (w) => w.id == _selectedWallet!.id,
        );
      }
    } else {
      _selectedWallet = null;
    }

    notifyListeners();
  }

  /// رفع وثائق التوثيق KYC
  Future<bool> submitKyc({
    required String frontBase64,
    required String backBase64,
  }) async {
    _isLoading = true;
    notifyListeners();

    final result = await ApiService.submitKyc(
      idFrontBase64: frontBase64,
      idBackBase64: backBase64,
    );

    _isLoading = false;
    if (result['success'] == true) {
      await refreshProfile();
      return true;
    }
    _errorMessage = result['message'];
    notifyListeners();
    return false;
  }

  /// تحديث إعدادات الخصوصية
  Future<bool> updatePrivacy({
    required bool hideFullName,
    required bool hidePhoneOnPos,
    required bool isBiometricEnabled,
  }) async {
    _isLoading = true;
    notifyListeners();

    final result = await ApiService.updatePrivacy(
      hideFullName: hideFullName,
      hidePhoneOnPos: hidePhoneOnPos,
      isBiometricEnabled: isBiometricEnabled,
    );

    _isLoading = false;
    if (result['success'] == true) {
      if (result['user'] != null) {
        _user = result['user'];
      } else {
        await refreshProfile();
      }
      notifyListeners();
      return true;
    }
    _errorMessage = result['message'];
    notifyListeners();
    return false;
  }

  /// تجديد الرقم البديل لنقاط البيع
  Future<String?> regeneratePosAlias() async {
    final result = await ApiService.regeneratePosAlias();
    if (result['success'] == true && result['newAlias'] != null) {
      await refreshProfile();
      return result['newAlias'];
    }
    return null;
  }

  /// تغيير كلمة المرور
  Future<bool> changePassword({
    String? currentPassword,
    required String newPassword,
  }) async {
    _isLoading = true;
    notifyListeners();

    final result = await ApiService.changePassword(
      currentPassword: currentPassword,
      newPassword: newPassword,
    );

    _isLoading = false;
    if (result['success'] == true) {
      await refreshProfile();
      return true;
    }
    _errorMessage = result['message'];
    notifyListeners();
    return false;
  }

  /// تسجيل الخروج
  Future<void> logout() async {
    await ApiService.clearToken();
    _user = null;
    _wallets = [];
    _selectedWallet = null;
    notifyListeners();
  }
}
