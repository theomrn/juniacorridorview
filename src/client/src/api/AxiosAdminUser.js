import axios from "axios";

export const createUser = async (email, password) => {
  try {
    const res = await axios.post("http://localhost:8000/api/create-user", { email, password });
    return res.data;
  } catch (error) {
    console.error("Error creating user:", error);
    throw error;
  }
};

export const getAllUsers = async () => {
  try {
    const res = await axios.get("http://localhost:8000/api/list-users");
    return res.data.users ?? [];
  } catch (error) {
    console.error("Error fetching users:", error);
    throw error;
  }
};

export const resetPassword = async (email) => {
  try {
    const res = await axios.post("http://localhost:8000/api/reset-password", { email });
    return res.data;
  } catch (error) {
    console.error("Error resetting password:", error);
    throw error;
  }
};

export const deleteUser = async (uid) => {
  try {
    const res = await axios.post("http://localhost:8000/api/delete-user", { uid });
    return res.data;
  } catch (error) {
    console.error("Error deleting user:", error);
    throw error;
  }
};
