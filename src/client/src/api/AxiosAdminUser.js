import axios from "axios";
import firebase from "firebase/compat/app";
import "firebase/compat/auth";

const getAuthHeader = async () => {
  const token = await firebase.auth().currentUser?.getIdToken(true);
  return token ? { Authorization: `Bearer ${token}` } : {};
};

export const createUser = async (email, password) => {
  const headers = await getAuthHeader();
  const res = await axios.post("/api/create-user", { email, password }, { headers });
  return res.data;
};

export const getAllUsers = async () => {
  const headers = await getAuthHeader();
  const res = await axios.get("/api/list-users", { headers });
  return res.data.users;
};

export const resetPassword = async (email) => {
  const headers = await getAuthHeader();
  const res = await axios.post("/api/reset-password", { email }, { headers });
  return res.data;
};

export const deleteUser = async (uid) => {
  const headers = await getAuthHeader();
  const res = await axios.post("/api/delete-user", { uid }, { headers });
  return res.data;
};

export const setAdminClaim = async (uid) => {
  const headers = await getAuthHeader();
  const res = await axios.post("/api/set-admin-claim", { uid }, { headers });
  return res.data;
};
