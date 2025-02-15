// https://github.com/kobake/leveldb-vc140-nuget
// Add this to linker addition definition leveldb.lib;shlwapi.lib; !!!!
// Add this to command com parameters of compiler "/ D_SCL_SECURE_NO_WARNINGS"
// Add this / D_CRT_SECURE_NO_WARNINGS


#include "stdafx.h"

#include <iostream>
#include <cassert>
#include <fstream>
#include <string>
#include <iostream>
#include <clocale>
#include <locale>
#include <codecvt>

#include "leveldb/db.h"
#include "leveldb/iterator.h"
#include "leveldb/options.h"
#include "leveldb/write_batch.h"

//#include "utf8.h"
//#include <vector>
//
//#include <list>

using namespace std;


void savetofile(string name, string value) {
	std::ofstream file(name);
	std::string my_string = value;
	file << my_string;
	file.close();
}

void savetofilew(string name, wstring value) {
	std::wofstream file(name);
	std::wstring my_string = value;
	file << my_string;
	file.close();
}

string openfromfile(string name) {
	std::ifstream file(name);
	std::string my_string = NULL;
	file >> my_string;
	file.close();
	return my_string;
}

wstring openfromfilew(string name) {
	std::wifstream file(name);
	std::wstring my_string = NULL;
	file >> my_string;
	file.close();
	return my_string;
}

extern "C"
{

	leveldb::DB* db;
	std::string v;
	unsigned char* buffer = NULL;

	__declspec(dllexport) bool DbOpen(char* path) {
		cout << "DbOpen path: " << path << endl;
		leveldb::Options options;
		options.create_if_missing = false;
		leveldb::Status status = leveldb::DB::Open(options, path, &db);
		if (status.ok()) {
			cout << "DbOpen Ok!" << endl;
			return true;
		}
		else {
			cout << "DbOpen Error!" << endl;
			return false;
		}
	}



	__declspec(dllexport) bool DbKeyOpen(char* key) {
		cout << "DbKeyOpen " << key << endl;
		std::string key_ = key;
		key_[52] = '\x0';
		key_[53] = '\x1';
		leveldb::Slice k = key_;
		

		leveldb::Status status = db->Get(leveldb::ReadOptions(), k, &v);
		if (status.ok()) {
			cout << "DbKeyOpen Ok!" << endl;
			//v.erase(0, 1); // Delete artifact. not forget about this when store back to DB !!!!!
			return true;
		}
		else {
			cout << "DbKeyOpen Error!" << endl;
			return false;
		}
	}

	// Updates and commits the new data in one batch.
	// PROBLEM::: Results in 2 chars added to resulting db. We see this when exporting after import. How is this happening?
	__declspec(dllexport) bool ApplyChangesToDb(char* key, char* val, size_t bufferLen) {
		cout << "ApplyChangesToDb " << key << endl;
		std::string key_ = key;

		std::string zbuffer =new char[bufferLen];

		// I know this is hacky, but not c++ expert and it works...
		int i = 0;
		for (i = 0; i < bufferLen; i++) {
			zbuffer[i] = val[i];
		}
		//zbuffer[i] = '\x0';
		//zbuffer[i+1] = '\x1';
		key_[52] = '\x0';
		key_[53] = '\x1';

		//zbuffer.append('\x0');
		//zbuffer.append('\x1');
	

		leveldb::Slice k = key_;
		leveldb::Slice v = leveldb::Slice(zbuffer);
		
		leveldb::WriteBatch batch;
		batch.Delete(k);
		cout << "Delete existing key " << key_ << endl;
		batch.Put(k, zbuffer);
		//cout << "Put new key " << key_ << "with value " << val_ << endl;
		cout << "Put new key " << key_ << endl;
		//cout << "Put new key X" << v.data.size() << endl;
		//cout << "Put new key XX" << zbuffer << endl;
		leveldb::Status s = db->Write(leveldb::WriteOptions(), &batch);
		if (s.ok()) {
			cout << "ApplyChangesToDb Ok!" << endl;
		}
		else {
			cout << "ApplyChangesToDb Error!" << endl;
		}
		
		//v.erase(0, 1); // Delete artifact. not forget about this when store back to DB !!!!!
		return true;
	}
	/*
	__declspec(dllexport) bool ApplyChangesToDb(char* key, char* val) {
		cout << "ApplyChangesToDb " << key << endl;
		std::string key_ = key;
		std::string val_ = val;
		key_[52] = '\x0';
		key_[53] = '\x1';
		leveldb::Slice k = key_;
		leveldb::Slice v = val_;

		leveldb::WriteBatch batch;
		batch.Delete(k);
		cout << "Delete existing key " << key_ << endl;
		batch.Put( k, v);
		//cout << "Put new key " << key_ << "with value " << val_ << endl;
		cout << "Put new key " << key_ << endl;
		cout << "Put new key " << val_.length() << endl;
		leveldb::Status s = db->Write(leveldb::WriteOptions(), &batch);
		if (s.ok()) {
			cout << "ApplyChangesToDb Ok!" << endl;
		}
		else {
			cout << "ApplyChangesToDb Error!" << endl;
		}

		//v.erase(0, 1); // Delete artifact. not forget about this when store back to DB !!!!!
		return true;
	}
	*/
	__declspec(dllexport) bool DbKeyPut(char* key, char* val) {
		cout << "DbKeyOpen " << key << endl;
		std::string key_ = key;
		std::string val_ = val;
		key_[52] = '\x0';
		key_[53] = '\x1';
		leveldb::Slice k = key_;
		leveldb::Slice v = val_;

		leveldb::Status status = db->Put(leveldb::WriteOptions(), k, v);
		if (status.ok()) {
			cout << "DbKeyPut Ok!" << endl;
			//v.erase(0, 1); // Delete artifact. not forget about this when store back to DB !!!!!
			return true;
		}
		else {
			cout << "DbKeyPut Error!" << endl;
			return false;
		}
	}

	__declspec(dllexport) bool DbWrite(char* key) {
		cout << "DbWrite " << key << endl;
		
		leveldb::WriteBatch updates;

		leveldb::Status status = db->Write(leveldb::WriteOptions(), &updates);
		if (status.ok()) {
			

			cout << "DbWrite Ok!" << endl;
			//v.erase(0, 1); // Delete artifact. not forget about this when store back to DB !!!!!
			return true;
		}
		else {
			cout << "DbWrite Error!" << endl;
			return false;
		}
	}
	/*// Set the database entry for "key" to "value".  Returns OK on success,
  // and a non-OK status on error.
  // Note: consider setting options.sync = true.
  virtual Status Put(const WriteOptions& options,
                     const Slice& key,
                     const Slice& value) = 0;

  // Remove the database entry (if any) for "key".  Returns OK on
  // success, and a non-OK status on error.  It is not an error if "key"
  // did not exist in the database.
  // Note: consider setting options.sync = true.
  virtual Status Delete(const WriteOptions& options, const Slice& key) = 0;

  // Apply the specified updates to the database.
  // Returns OK on success, non-OK on failure.
  // Note: consider setting options.sync = true.
  virtual Status Write(const WriteOptions& options, WriteBatch* updates) = 0;*/
	
	/*
	unsigned char* xxbuffer = NULL;

	__declspec(dllexport) void VSize(size_t *bufferLen)
	{
		// we found the key, so set the buffer length 
		//*bufferLen = v.size();
		*bufferLen = sizeof xxbuffer;
	}
	*/

	// Creates a buffer, copies val data to it, then returns a pointer to it.
	__declspec(dllexport) const unsigned char* DbGet(size_t *bufferLen)
	{

		// we found the key, so set the buffer length 
		*bufferLen = v.size();

		// initialize the buffer
		buffer = new unsigned char[*bufferLen];

		// set the buffer
		memset(buffer, 0, *bufferLen);

		// copy the data
		memcpy((void*)(buffer), v.c_str(), *bufferLen);

		cout << "===>> " << *bufferLen  << endl;
		
		return buffer;
	}


	

/*
	// Creates a buffer, reads val data from it, then returns a pointer to it.
	__declspec(dllexport) const unsigned char* DbSet3( size_t bufferLen)
	{

		// 
		//*bufferLen = newStr.size();

		// copy newStr to v.

		unsigned char* xbuffer = NULL;

		// initialize the buffer
		xbuffer = new unsigned char[bufferLen];

		// set the buffer
		memset(xbuffer, 0, bufferLen);

		// copy the data
		//memcpy((void*)(xbuffer), newStr, bufferLen);

		
		//cout << "===>> " << bufferLen << endl;
		xxbuffer = xbuffer;
		return xbuffer;
	}
	

	// Set v to string value that we pass.
	__declspec(dllexport) const void DbSet(std::string newStr)
	{

		// 
		size_t bufferLen = newStr.size();

		// copy newStr to v.

		unsigned char* xbuffer = NULL;

		// initialize the buffer
		xbuffer = new unsigned char[bufferLen];

		// set the buffer
		memset(xbuffer, 0, bufferLen);

		// copy the data
		memcpy((void*)(xbuffer), newStr.c_str(), bufferLen);

		cout << "===>> " << bufferLen << endl;

		//return xbuffer;
	}

	// Set v to string value that we pass.
	__declspec(dllexport) const void DbSet2(std::string newStr, size_t *bufferLen)
	{

		// 
		*bufferLen = newStr.size();

		// copy newStr to v.

		unsigned char* xbuffer = NULL;

		// initialize the buffer
		xbuffer = new unsigned char[*bufferLen];

		// set the buffer
		memset(xbuffer, 0, *bufferLen);

		// copy the data
		memcpy((void*)(xbuffer), newStr.c_str(), *bufferLen);

		cout << "===>> " << *bufferLen << endl;

		//return xbuffer;
	}
	*/

	__declspec(dllexport) bool DbSaveBinary(char* s) {
		cout << "DbSaveBinary Ok!" << endl;
		savetofile(s, v);
		return true;
	}

	__declspec(dllexport) std::string DbReadBinary(char* s) {
		cout << "DbReadBinary Ok!" << endl;
		return openfromfile(s);
	}

	__declspec(dllexport) int DbKeyClose() {
		cout << "DbKeyClose Ok!" << endl;
		delete[] buffer;
		return 0;
	}


	__declspec(dllexport) int DbClose(int i) {
		cout << "DbClose Ok!" << endl;
		delete db;
		return 0;
	}


	



}



